using System.Collections.Generic;
using UnityEngine;
using System;

public class SMEvent
{
    readonly string name;
    readonly List<string> froms;
    readonly string to;

    public string Name { get { return name; } }
    public List<string> Froms { get { return froms; } }
    public string To { get { return to; } }

    public SMEvent(string _name, List<string> _froms, string _to)
    {
        name = _name;
        froms = _froms;
        to = _to;
    }
}

public class StateMachine : MonoBehaviour
{

    // the event transitioned successfully from one state to another
    const int SUCCEEDED = 1;
    // the event was successfull but no state transition was necessary
    const int NOTRANSITION = 2;
    // the event was cancelled by the caller in a beforeEvent callback
    const int CANCELLED = 3;
    // the event is asynchronous and the caller is in control of when the transition occurs
    const int PENDING = 4;
    // the event was failure
    const int FAILURE = 5;

    // caller tried to fire an event that was innapropriate in the current state
    const string INVALID_TRANSITION_ERROR = "INVALID_TRANSITION_ERROR";
    // caller tried to fire an event while an async transition was still pending
    const string PENDING_TRANSITION_ERROR = "PENDING_TRANSITION_ERROR";
    // caller provided callback function threw an exception
    const string INVALID_CALLBACK_ERROR = "INVALID_CALLBACK_ERROR";

    const string WILDCARD = "*";
    const string ASYNC = "ASYNC";

    const string STATE_NONE = "none";

    public static string HANDLE_ONBRFOREEVENT = "onbeforeevent";
    public static string HANDLE_ONAFTEREVENT = "onafterevent";
    public static string HANDLE_ONLEAVEEVENT = "onleavestate";
    public static string HANDLE_ONENTEREVENT = "onenterstate";
    public static string HANDLE_ONCHANGEEVENT = "onchangestate";

    Dictionary<string, Dictionary<string, string>> _map;
    Dictionary<string, Func<SMEvent, bool>> _callbacks;

    // private:
    string _curState;
    string _terminal;

    bool _inTransition;

    public StateMachine SetupState(List<SMEvent> events, Dictionary<string, Func<SMEvent, bool>> callbacks, string initial, string terminal, bool defer)
    {
        _callbacks = callbacks;
        _terminal = terminal;

        _map = new Dictionary<string, Dictionary<string, string>>();
        _curState = STATE_NONE;
        _inTransition = false;


        SMEvent initialEvent = new SMEvent(initial, new List<string> { STATE_NONE }, initial);
        AddEvent(initialEvent);

        foreach (SMEvent smEvent in events)
        {
            AddEvent(smEvent);
        }

        if (false == defer)
        {
            DoEvent(initialEvent.Name);
        }

        return this;
    }

    public bool IsReady()
    {
        return _curState != STATE_NONE;
    }

    public string GetState()
    {
        return _curState;
    }

    public bool IsState(string state)
    {
        return _curState == state;
    }

    public bool CanDoEvent(string eventName)
    {
        return !_inTransition && (_map.ContainsKey(eventName) && _map[eventName].ContainsKey(_curState));
    }

    public bool CanNotDoEvent(string eventName)
    {
        return !CanDoEvent(eventName);
    }

    public bool IsFinishedState()
    {
        return IsState(_terminal);
    }

    public int DoEventForce(string name)
    {
        string from = _curState;
        Dictionary<string, string> map = _map[name];
        string to = map[_curState];
        List<string> newFrom = new List<string> { from };
        SMEvent smEvent = new SMEvent(name, newFrom, to);

        if (true == _inTransition)
        {
            _inTransition = false;
        }

        BeforeEvent(smEvent);
        if (from == to)
        {
            AfterEvent(smEvent);
            return NOTRANSITION;
        }

        _curState = to;
        EnterEvent(smEvent);
        ChangeAnyState(smEvent);
        AfterEvent(smEvent);

        return SUCCEEDED;
    }

    public int DoEvent(string name)
    {
        if (false == _map.ContainsKey(name))
        {
            Debug.LogError(string.Format("StateMachine:doEvent() - invalid event {0}", name));
        }

        if (true == _inTransition)
        {
            //OnError(name, from, to, PENDING_TRANSITION_ERROR, string.Format("event:{0} inappropriate because previous transition did not complete", name));
            return FAILURE;
        }

        if (true == CanNotDoEvent(name))
        {
            //OnError(name, from, to, PENDING_TRANSITION_ERROR, string.Format("event:{0} inappropriate in current state", name, _curState));
            return FAILURE;
        }

        string from = _curState;
        Dictionary<string, string> map = _map[name];
        string to = map[_curState];
        List<string> newFrom = new List<string> { from };
        SMEvent smEvent = new SMEvent(name, newFrom, to);

        if (false == BeforeEvent(smEvent))
        {
            return CANCELLED;
        }

        if (from == to)
        {
            AfterEvent(smEvent);
            return NOTRANSITION;
        }

        _inTransition = true;
        if (false == LeaveState(smEvent))
        {
            _inTransition = false;
            return CANCELLED;
        }
        else
        {
            _inTransition = false;
            _curState = to;
            EnterEvent(smEvent);
            ChangeAnyState(smEvent);
            AfterEvent(smEvent);
            return SUCCEEDED;
        }
    }

    private void AddEvent(SMEvent smEvent) {
        if (false == _map.ContainsKey(smEvent.Name)) {
            _map.Add(smEvent.Name, new Dictionary<string, string>());
        }

        foreach (string fromName in smEvent.Froms) {
            _map[smEvent.Name].Add(fromName, smEvent.To);
        }
    }

    private bool DoCallback(Func<SMEvent, bool> callback, SMEvent smEvent)
    {
        if (null != callback)
        {
            return callback(smEvent);
        }

        return false;
    }

    private bool BeforeAnyEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(HANDLE_ONBRFOREEVENT))
            return true;
        return DoCallback(_callbacks[HANDLE_ONBRFOREEVENT], smEvent);
    }

    private bool AfterAnyEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(HANDLE_ONAFTEREVENT))
            return true;
        return DoCallback(_callbacks[HANDLE_ONAFTEREVENT], smEvent);
    }

    private bool EnterAnyState(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(HANDLE_ONENTEREVENT))
            return true;
        return DoCallback(_callbacks[HANDLE_ONENTEREVENT], smEvent);
    }

    private bool LeaveAnyState(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(HANDLE_ONLEAVEEVENT))
            return true;
        return DoCallback(_callbacks[HANDLE_ONLEAVEEVENT], smEvent);
    }

    private bool ChangeAnyState(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(HANDLE_ONCHANGEEVENT))
            return true;
        return DoCallback(_callbacks[HANDLE_ONCHANGEEVENT], smEvent);
    }

    private bool BeforeThisEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(string.Format("onbefore{0}", smEvent.Name)))
            return true;
        return DoCallback(_callbacks[string.Format("onbefore{0}", smEvent.Name)], smEvent);
    }

    private bool AfterThisEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(string.Format("onafter{0}", smEvent.Name)))
            return true;
        return DoCallback(_callbacks[string.Format("onafter{0}", smEvent.Name)], smEvent);
    }

    private bool LeaveThisEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(string.Format("onleave{0}", smEvent.Name)))
            return true;
        return DoCallback(_callbacks[string.Format("onleave{0}", smEvent.Name)], smEvent);
    }

    private bool EnterThisEvent(SMEvent smEvent)
    {
        if (false == _callbacks.ContainsKey(string.Format("onenter{0}", smEvent.Name)))
            return true;
        return DoCallback(_callbacks[string.Format("onenter{0}", smEvent.Name)], smEvent);
    }

    private bool BeforeEvent(SMEvent smEvent)
    {
        bool specific = BeforeThisEvent(smEvent);
        bool general = BeforeAnyEvent(smEvent);

        return specific && general;
    }

    private void AfterEvent(SMEvent smEvent)
    {
        AfterThisEvent(smEvent);
        AfterAnyEvent(smEvent);
    }

    private bool LeaveState(SMEvent smEvent)
    {
        bool specific = LeaveThisEvent(smEvent);
        bool general = LeaveAnyState(smEvent);
        return specific && general;
    }

    private void EnterEvent(SMEvent smEvent)
    {
        EnterThisEvent(smEvent);
        AfterAnyEvent(smEvent);
    }

    private void OnError(string eventName, string eventFrom, string eventTo, string error, string message)
    {
        Debug.Log(string.Format("[StateMachine] ERROR: error {0}, event {0}, from {0} to {0}", error, eventName, eventFrom, eventTo));
        Debug.LogError(message);
    }
}
