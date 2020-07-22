using System;

namespace KspTrigger
{
    public class TriggerActionMessage : TriggerAction
    {
        [Persistent(name="message")]
        private string _message;
        
        public TriggerActionMessage(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerActionType.Message;
            _message = "";
        }

        public TriggerActionMessage(TriggerActionMessage other) : base(other)
        {
            _type = TriggerActionType.Message;
            _message = other._message;
            UpdatePersistentData();
        }
        
        public override void LoadPersistentData(TriggerConfig triggerConfig) {}
        
        public override void UpdatePersistentData() {}
        
        public string Message
        {
            get { return _message; }
            set
            {
                if (value != _message)
                {
                    _modified = true;
                }
                _message = value;
            }
        }
        
        public override bool IsValid()
        {
            return true;
        }
        
        public override void DoAction()
        {
            ScreenMessages.PostScreenMessage(_message, 2f, ScreenMessageStyle.UPPER_CENTER);
        }
        
        public override string ToString()
        {
            return "Message display : " + _message;
        }
    }
}

