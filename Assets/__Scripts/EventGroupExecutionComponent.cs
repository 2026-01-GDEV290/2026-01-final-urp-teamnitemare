using System;
using System.Collections.Generic;
using UnityEngine;

//TODO: ISaveable implementation? (save place in execution order, # of groups and # of completed groups)

[Serializable]
public struct EventGroup
{
    public float delayBeforeExecution;
    public string groupName;
    public UnityEngine.Events.UnityEvent events;
    public bool continueToNextGroupAfterExecution;

    public bool completed;
}

public class EventGroupExecutionComponent : MonoBehaviour
{
    public List<EventGroup> eventGroups = new List<EventGroup>();

    int currentGroupIndex = 0;
    string currentGroupName = "";

    void Awake()
    {
        if (eventGroups.Count > 0)
        {
            currentGroupName = eventGroups[0].groupName;
            // set completed to false for all groups at start
            for (int i = 0; i < eventGroups.Count; i++)
            {
                var group = eventGroups[i];
                group.completed = false;
                eventGroups[i] = group;
            }
        }
    }

    public int GetCurrentGroupIndex()
    {
        return currentGroupIndex;
    }
    public string GetCurrentGroupName()
    {
        return currentGroupName;
    }

    public void ExecuteGroupAtStart()
    {
        ExecuteGroupAtIndex(0);
    }
    public void ExecuteGroupAtEnd()
    {
        ExecuteGroupAtIndex(eventGroups.Count - 1);
    }

    public void ExecuteGroupAtIndex(int index)
    {
        if (index >= 0 && index < eventGroups.Count)
        {
            currentGroupIndex = index;
            //currentGroupName = eventGroups[currentGroupIndex].groupName;
            ExecuteCurrentGroup();
        }
    }

    public void ExecuteGroupByName(string groupName)
    {
        int index = eventGroups.FindIndex(g => g.groupName == groupName);
        if (index != -1)
        {
            currentGroupIndex = index;
            //currentGroupName = eventGroups[currentGroupIndex].groupName;
            ExecuteCurrentGroup();
        }
    }
    public void ExecuteNextGroup()
    {
        if (currentGroupIndex + 1 < eventGroups.Count)
        {
            currentGroupIndex++;
            //currentGroupName = eventGroups[currentGroupIndex].groupName;
            ExecuteCurrentGroup();
        }
    }
    public void ExecuteCurrentGroup()
    {
        if (currentGroupIndex < eventGroups.Count)
        {
            var group = eventGroups[currentGroupIndex];
            currentGroupName = group.groupName;
            if (group.delayBeforeExecution > 0.001)
            {
                Invoke(nameof(ExecuteGroupEvents), group.delayBeforeExecution);
            }
            else
            {
                ExecuteGroupEvents();
            }
        }
    }
    public void CancelCurrentGroupQueue()
    {
        CancelInvoke(nameof(ExecuteGroupEvents));
    }

    private void ExecuteGroupEvents()
    {
        if (currentGroupIndex < eventGroups.Count)
        {
            var group = eventGroups[currentGroupIndex];
            group.events.Invoke();
            group.completed = true;
            if (group.continueToNextGroupAfterExecution)
            {
                ExecuteNextGroup();
            }
        }
    }
}
