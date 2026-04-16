using UnityEngine;
using System.Collections.Generic;

public class AudioVisualPulse : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private Camera cam;

	[Header("Hollow Circles")]
	[SerializeField] private Material ringMaterial;
	[SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.9f);
	[SerializeField] private float lineWidth = 0.03f;
	[SerializeField] private int circleSegments = 48;
	[SerializeField] private int poolSize = 20;
	[SerializeField] private int circlesPerPulse = 5;

	[Header("Timing")]
	[SerializeField] private float pulseInterval = 1.0f;
	[SerializeField] private float attractionSoundInterval = 2.0f;
	[SerializeField] private float circleLifetime = 0.45f;
	[SerializeField] private float circleStagger = 0.05f;

	[Header("Facing")]
	[SerializeField] private float facingThreshold = 0.45f;

	[Header("Screen Spawn")]
	[SerializeField] private float sideInset = 0.08f;
	[SerializeField] private float verticalClamp = 0.1f;
	[SerializeField] private float spawnDepth = 7f;

	[Header("Motion")]
	[SerializeField] private float startRadius = 0.15f;
	[SerializeField] private float endRadius = 1.2f;
	[SerializeField] private float driftDistance = 0.8f;
	[SerializeField] private float rotationOffsetDegrees = 0f;

	private readonly List<LineRenderer> ringPool = new List<LineRenderer>();
	private readonly Dictionary<LineRenderer, Coroutine> running = new Dictionary<LineRenderer, Coroutine>();
	private List<PitchBlackAttraction> activeAttractions = new List<PitchBlackAttraction>();
	private int nextIndex;
	private float nextPulseTime;
	private float nextAttractionSoundTime;
	private bool wasFacingTarget;

	void Awake()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}

		BuildPool();
		RefreshAttractions();
		nextPulseTime = Time.time;
		nextAttractionSoundTime = Time.time;
	}

	void Update()
	{
		if (cam == null || ringPool.Count == 0)
		{
			return;
		}

		if (Time.time < nextPulseTime)
		{
			return;
		}

		nextPulseTime = Time.time + pulseInterval;
		RefreshAttractions();

		PitchBlackAttraction closest = GetClosestAttraction();
		if (closest == null)
		{
			return;
		}

		bool isFacingTarget = IsFacingTarget(closest.transform.position);
		if (isFacingTarget)
		{
			wasFacingTarget = true;
			nextAttractionSoundTime = Time.time;
			return;
		}

		if (wasFacingTarget)
		{
			wasFacingTarget = false;
			nextPulseTime = Time.time;
			nextAttractionSoundTime = Time.time;
		}

		if (Time.time >= nextAttractionSoundTime)
		{
			closest.PlaySound();
			nextAttractionSoundTime = Time.time + Mathf.Max(0.05f, attractionSoundInterval);
		}

		EmitPulse(closest.transform);
	}

	void BuildPool()
	{
		int count = Mathf.Max(1, poolSize);
		int segments = Mathf.Max(12, circleSegments);

		for (int i = 0; i < count; i++)
		{
			GameObject go = new GameObject("PulseRing_" + i);
			go.transform.SetParent(transform, false);

			LineRenderer lr = go.AddComponent<LineRenderer>();
			lr.useWorldSpace = true;
			lr.loop = true;
			lr.positionCount = segments;
			lr.startWidth = lineWidth;
			lr.endWidth = lineWidth;
			lr.alignment = LineAlignment.View;
			lr.numCapVertices = 2;
			lr.numCornerVertices = 4;

			if (ringMaterial != null)
			{
				lr.material = ringMaterial;
			}
			else
			{
				lr.material = new Material(Shader.Find("Sprites/Default"));
			}

			lr.startColor = ringColor;
			lr.endColor = ringColor;
			go.SetActive(false);

			ringPool.Add(lr);
		}
	}

	void RefreshAttractions()
	{
		activeAttractions = new List<PitchBlackAttraction>(FindObjectsByType<PitchBlackAttraction>(FindObjectsSortMode.None));
	}

	PitchBlackAttraction GetClosestAttraction()
	{
		PitchBlackAttraction best = null;
		float bestSqr = float.MaxValue;
		Vector3 camPos = cam.transform.position;

		for (int i = 0; i < activeAttractions.Count; i++)
		{
			PitchBlackAttraction a = activeAttractions[i];
			if (a == null || !a.gameObject.activeInHierarchy)
			{
				continue;
			}

			float sqr = (a.transform.position - camPos).sqrMagnitude;
			if (sqr < bestSqr)
			{
				bestSqr = sqr;
				best = a;
			}
		}

		return best;
	}

	bool IsFacingTarget(Vector3 targetWorld)
	{
		Vector3 toTarget = (targetWorld - cam.transform.position).normalized;
		float dot = Vector3.Dot(cam.transform.forward, toTarget);
		return dot > facingThreshold;
	}

	void EmitPulse(Transform target)
	{
		Vector3 toTargetWorld = (target.position - cam.transform.position).normalized;
		Vector3 toTargetLocal = cam.transform.InverseTransformDirection(toTargetWorld);

		Vector2 turnHint = new Vector2(toTargetLocal.x, toTargetLocal.y);
		if (turnHint.sqrMagnitude < 0.0001f)
		{
			// Fallback to screen-center offset if the local hint is too small.
			Vector3 vp = cam.WorldToViewportPoint(target.position);
			turnHint = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
		}

		turnHint = turnHint.sqrMagnitude > 0.0001f ? turnHint.normalized : Vector2.right;
		bool fromLeft = turnHint.x < 0f;

		float x = fromLeft ? sideInset : 1f - sideInset;
		float y = Mathf.Clamp(0.5f + Mathf.Clamp(turnHint.y, -1f, 1f) * 0.35f, verticalClamp, 1f - verticalClamp);
		Vector3 startCenter = cam.ViewportToWorldPoint(new Vector3(x, y, spawnDepth));

		Vector3 targetDir = cam.transform.right * turnHint.x + cam.transform.up * turnHint.y;
		if (targetDir.sqrMagnitude < 0.0001f)
		{
			targetDir = fromLeft ? cam.transform.right : -cam.transform.right;
		}

		targetDir.Normalize();
		targetDir = Quaternion.AngleAxis(rotationOffsetDegrees, cam.transform.forward) * targetDir;

		int count = Mathf.Clamp(circlesPerPulse, 1, ringPool.Count);
		for (int i = 0; i < count; i++)
		{
			LineRenderer lr = GetNextRing();
			if (lr == null)
			{
				break;
			}

			if (running.TryGetValue(lr, out Coroutine active) && active != null)
			{
				StopCoroutine(active);
			}

			float delay = i * circleStagger;
			Coroutine c = StartCoroutine(AnimateCircle(lr, startCenter, targetDir, delay));
			running[lr] = c;
		}
	}

	LineRenderer GetNextRing()
	{
		if (ringPool.Count == 0)
		{
			return null;
		}

		LineRenderer lr = ringPool[nextIndex];
		nextIndex = (nextIndex + 1) % ringPool.Count;
		return lr;
	}

	void DrawCircle(LineRenderer lr, Vector3 center, float radius)
	{
		int segments = lr.positionCount;
		Vector3 right = cam.transform.right;
		Vector3 up = cam.transform.up;

		for (int i = 0; i < segments; i++)
		{
			float t = (float)i / segments;
			float angle = t * Mathf.PI * 2f;
			Vector3 p = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
			lr.SetPosition(i, p);
		}
	}

	System.Collections.IEnumerator AnimateCircle(LineRenderer lr, Vector3 startCenter, Vector3 driftDir, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}

		GameObject go = lr.gameObject;
		go.SetActive(true);

		float elapsed = 0f;
		while (elapsed < circleLifetime)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / circleLifetime);
			float eased = Mathf.SmoothStep(0f, 1f, t);

			float radius = Mathf.Lerp(startRadius, endRadius, eased);
			Vector3 center = startCenter + driftDir * (driftDistance * eased);
			DrawCircle(lr, center, radius);

			float alpha = 1f - eased;
			Color c = ringColor;
			c.a *= alpha;
			lr.startColor = c;
			lr.endColor = c;

			yield return null;
		}

		go.SetActive(false);
		running[lr] = null;
	}
}