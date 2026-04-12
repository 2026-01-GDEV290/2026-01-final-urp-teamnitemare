using System;
using UnityEngine;

// TODO: Collectibles with same-name, count etc implementation

[Serializable]
public class QuestComponent : MonoBehaviour
{
    // just need an id & collectible bool
    public string questComponentId = "";
    public bool isCollectible = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(questComponentId))
        {
            if (isCollectible)
            {
                questComponentId = "COLLECTIBLE:" + gameObject.name;
            }
            else
            {
                questComponentId = gameObject.name;
            }
        }
    }
    void OnValidate()
    {
        // make sure COLLECTIBLE prefix is there if collectible
        if (isCollectible && !questComponentId.StartsWith("COLLECTIBLE:"))
        {
            questComponentId = "COLLECTIBLE:" + questComponentId;
        }
        else if (!isCollectible && questComponentId.StartsWith("COLLECTIBLE:"))
        {
            isCollectible = true;
        }
    }

    public void CompleteTask()
    {
        QuestManager.Instance.CompleteTaskObjectForUnknownQuest(this);
    }
}