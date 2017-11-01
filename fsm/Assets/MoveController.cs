using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour {

    StateMachine fsm;

    void Start () {
        InitFSM();
    }
	
	void Update () {
        bool move = Input.GetKeyDown(KeyCode.W);
        bool attack = Input.GetKey(KeyCode.A);
        bool normal = Input.GetKeyUp(KeyCode.D);
        bool lie = Input.GetKeyUp(KeyCode.S);

        if (move)
        {
            fsm.DoEvent("move");
        }
        else if (attack)
        {
            fsm.DoEvent("attack");
        }
        else if (normal)
        {
            fsm.DoEvent("normal");
        }
        else if (lie)
        {
            fsm.DoEvent("lie");
        }

    }

    void InitFSM() {
        fsm = gameObject.AddComponent<StateMachine>();

        List<SMEvent> events = new List<SMEvent>();
        events.Add(new SMEvent("move", new List<string> { "idle", "jump"}, "walk"));
        events.Add(new SMEvent("attack", new List<string> { "idle", "walk" }, "jump"));
        events.Add(new SMEvent("normal", new List<string> { "walk", "jump"}, "idle"));
        events.Add(new SMEvent("lie", new List<string> { "move", "idle"}, "falldown"));

        Func<SMEvent, bool> onenteridle = x => Walk(x);
        Func<SMEvent, bool> onenterwalk = x => Jump(x);
        Func<SMEvent, bool> onenterjump = x => Idle(x);
        Func<SMEvent, bool> onenterlie = x => FallDown(x);

        Dictionary<string, Func<SMEvent, bool>> callbacks = new Dictionary<string, Func<SMEvent, bool>>();
        callbacks.Add("onenternormal", onenteridle);
        callbacks.Add("onentermove", onenterwalk);
        callbacks.Add("onenterattack", onenterjump);
        callbacks.Add("onenterlie", onenterlie);

        string initial = "idle";
        fsm.SetupState(events, callbacks, initial, "", false);
    }


    private bool Walk(SMEvent smEvent) {
        Debug.LogError("Walk");
        return true;
    }

    private bool Jump(SMEvent smEvent)
    {
        Debug.LogError("Jump");
        return true;
    }

    private bool Idle(SMEvent smEvent)
    {
        Debug.LogError("Idle");
        return true;
    }

    private bool FallDown(SMEvent smEvent)
    {
        Debug.LogError("FallDown");
        return true;
    }
}
