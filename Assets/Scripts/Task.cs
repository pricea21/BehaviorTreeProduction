﻿using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Task
{
    public abstract void run();
    public bool succeeded;

    protected int eventId;
    const string EVENT_NAME_PREFIX = "FinishedTask";
    public string TaskFinished
    {
        get
        {
            return EVENT_NAME_PREFIX + eventId;
        }
    }
    public Task()
    {
        eventId = EventBus.GetEventID();
    }
}

public class IsTrue : Task
{
    bool varToTest;

    public IsTrue(bool someBool)
    {
        varToTest = someBool;
        
    }

    public override void run()
    {
        succeeded = varToTest;
        EventBus.TriggerEvent(TaskFinished);
    }
}


public class IsFalse : Task
{
    bool varToTest;

    public IsFalse(bool someBool)
    {
        varToTest = someBool;
    }

    public override void run()
    {
        succeeded = !varToTest;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class OpenDoor : Task
{
    Door mDoor;

    public OpenDoor(Door someDoor)
    {
        mDoor = someDoor;
    }

    public override void run()
    {
        succeeded = mDoor.Open();
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class BargeDoor : Task
{
    Rigidbody mDoor;

    public BargeDoor(Rigidbody someDoor)
    {
        mDoor = someDoor;
    }

    public override void run()
    {
        mDoor.AddForce(-20f, 0, 0, ForceMode.VelocityChange);
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class HulkOut : Task
{
    GameObject mEntity;

    public HulkOut(GameObject someEntity)
    {
        mEntity = someEntity;
    }

    public override void run()
    {
        mEntity.transform.localScale *= 3;
        mEntity.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class Jumping : Task
{
    GameObject mEntity;

    public Jumping(GameObject someEntity)
    {
        mEntity = someEntity;
    }

    public override void run()
    {
        mEntity.transform.localScale *= .5f;
        mEntity.GetComponent<Rigidbody>().AddForce(Vector3.up * 500.0f);
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}
public class WhiteOut : Task
{
    GameObject mEntity;

    public WhiteOut(GameObject someEntity)
    {
        mEntity = someEntity;
    }

    public override void run()
    {
        mEntity.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class Ghost : Task
{
    Door mDoor;

    public Ghost(Door someDoor)
    {
        mDoor = someDoor;
    }

    public override void run()
    {
        Debug.Log("Going Ghost");
        mDoor.GetComponentInChildren<Collider>().enabled = false;
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class ChangeBack : Task
{
    GameObject mEntity;

    public ChangeBack(GameObject someEntity)
    {
        mEntity = someEntity;
    }

    public override void run()
    {
        mEntity.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class Wait : Task
{
    float mTimeToWait;

    public Wait(float time)
    {
        mTimeToWait = time;
    }

    public override void run()
    {
        succeeded = true;
        EventBus.ScheduleTrigger(TaskFinished, mTimeToWait);
    }
}

public class MoveKinematicToObject : Task
{
    Arriver mMover;
    GameObject mTarget;

    public MoveKinematicToObject(Kinematic mover, GameObject target)
    {
        mMover = mover as Arriver;
        mTarget = target;

    }

    public override void run()
    {
        //Debug.Log("Moving to target position: " + mTarget);
        mMover.OnArrived += MoverArrived;
        mMover.myTarget = mTarget;
    }

    public void MoverArrived()
    {
        //Debug.Log("arrived at " + mTarget);
        mMover.OnArrived -= MoverArrived;
        succeeded = true;
        EventBus.TriggerEvent(TaskFinished);
    }
}

public class Sequence : Task
{
    List<Task> children;
    Task currentTask;
    int currentTaskIndex = 0;

    public Sequence(List<Task> taskList)
    {
        children = taskList;
    }

    // Sequence wants all tasks to succeed
    // try all tasks in order
    // stop and return false on the first task that fails
    // return true if all tasks succeed
    public override void run()
    {
        //Debug.Log("sequence running child task #" + currentTaskIndex);
        currentTask = children[currentTaskIndex];
        EventBus.StartListening(currentTask.TaskFinished, OnChildTaskFinished);
        currentTask.run();
    }

    void OnChildTaskFinished()
    {
        //Debug.Log("Behavior complete! Success = " + currentTask.succeeded);
        if (currentTask.succeeded)
        {
            EventBus.StopListening(currentTask.TaskFinished, OnChildTaskFinished);
            currentTaskIndex++;
            if (currentTaskIndex < children.Count)
            {
                this.run();
            }
            else
            {
                // we've reached the end of our children and all have succeeded!
                succeeded = true;
                EventBus.TriggerEvent(TaskFinished);
            }

        }
        else
        {
            // sequence needs all children to succeed
            // a child task failed, so we're done
            succeeded = false;
            EventBus.TriggerEvent(TaskFinished);
        }
    }
}

public class Selector : Task
{
    List<Task> children;
    Task currentTask;
    int currentTaskIndex = 0;

    public Selector(List<Task> taskList)
    {
        children = taskList;
    }

    // Selector wants only the first task that succeeds
    // try all tasks in order
    // stop and return true on the first task that succeeds
    // return false if all tasks fail
    public override void run()
    {
        //Debug.Log("selector running child task #" + currentTaskIndex);
        currentTask = children[currentTaskIndex];
        EventBus.StartListening(currentTask.TaskFinished, OnChildTaskFinished);
        currentTask.run();
    }

    void OnChildTaskFinished()
    {
        //Debug.Log("Behavior complete! Success = " + currentTask.succeeded);
        if (currentTask.succeeded)
        {
            succeeded = true;
            EventBus.TriggerEvent(TaskFinished);
        }
        else
        {
            EventBus.StopListening(currentTask.TaskFinished, OnChildTaskFinished);
            currentTaskIndex++;
            if (currentTaskIndex < children.Count)
            {
                this.run();
            }
            else
            {
                // we've reached the end of our children and none have succeeded!
                succeeded = false;
                EventBus.TriggerEvent(TaskFinished);
            }
        }
    }
}

