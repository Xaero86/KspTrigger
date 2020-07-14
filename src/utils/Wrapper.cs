using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class Autopilot
    {
        private FlightCtrlState _fcs;
        private double _until;
        
        public Autopilot(Vessel vessel)
        {
            vessel.OnFlyByWire += new FlightInputCallback(InputCallback);
            _fcs = new FlightCtrlState();
            _until = 0.0;
        }
        
        public void Throttle(float throttle)
        {
            FlightInputHandler.state.mainThrottle = throttle;
        }
        
        public void Rotate(float yaw, float pitch, float roll, double duration)
        {
            _fcs.yaw = yaw;
            _fcs.pitch = pitch;
            _fcs.roll = roll;
            _until = Planetarium.GetUniversalTime() + duration;
        }
        
        public void Translate(float X, float Y, float Z, double duration)
        {
            _fcs.X = X;
            _fcs.Y = Y;
            _fcs.Z = Z;
            _until = Planetarium.GetUniversalTime() + duration;
        }
        
        public void WheelSteer(float wheelSteer, double duration)
        {
            _fcs.wheelSteer = wheelSteer;
            _until = Planetarium.GetUniversalTime() + duration;
        }
        
        public void WheelThrottle(float wheelThrottle, double duration)
        {
            _fcs.wheelThrottle = wheelThrottle;
            _until = Planetarium.GetUniversalTime() + duration;
        }
        
        public void InputCallback(FlightCtrlState fcs)
        {
            if (_until > Planetarium.GetUniversalTime())
            {
                fcs.yaw = _fcs.yaw;
                fcs.pitch = _fcs.pitch;
                fcs.roll = _fcs.roll;
                fcs.X = _fcs.X;
                fcs.Y = _fcs.Y;
                fcs.Z = _fcs.Z;
                fcs.wheelSteer = _fcs.wheelSteer;
                fcs.wheelThrottle = _fcs.wheelThrottle;
            }
        }
    }
    
    public class PartWrapper
    {
        private Part _part;
        
        public PartWrapper(Part part)
        {
            _part = part;
        }
        
        public double GetResourceAmount(int Resource)
        {
            double result = 0.0;
            PartResource partResource = (_part != null) ? ((_part.Resources != null) ? _part.Resources.Get(Resource) : null) : null;
            if (partResource != null)
            {
                result = partResource.amount;
            }
            return result;
        }
        
        public double GetResourceAmountPercent(int Resource)
        {
            double result = 0.0;
            PartResource partResource = (_part != null) ? ((_part.Resources != null) ? _part.Resources.Get(Resource) : null) : null;
            if (partResource != null)
            {
                result = 100.0 * partResource.amount / partResource.maxAmount;
            }
            return result;
        }
    }
    
    public class VesselWrapper
    {
        public static readonly Dictionary<string,object> ResourceDict = new Dictionary<string,object>();
        private Vessel _vessel;
        
        static VesselWrapper()
        {
            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                ResourceDict.Add(resource.name, resource.id);
            }
        }
        
        public VesselWrapper(Vessel vessel)
        {
            _vessel = vessel;
        }
        
        public void CustomGroup(int GroupNum)
        {
            if ((GroupNum > 0) && (GroupNum <= 10))
            {
                KSPActionGroup action = (KSPActionGroup) (GroupNum + (int) KSPActionGroup.Custom01 - 1);
                _vessel.ActionGroups.SetGroup(action, true);
            }
        }
        
        public float OrbitalSpeed()
        {
            return _vessel.GetObtVelocity().magnitude;
        }
        
        public float GroundSpeed()
        {
            return _vessel.GetSrfVelocity().magnitude;
        }
        
        public double GetResourceAmount(int Resource)
        {
            double amount = 0.0;
            double maxAmount = 1.0;
            _vessel.GetConnectedResourceTotals(Resource, true, out amount, out maxAmount);
            return amount;
        }
        
        public double GetResourceAmountPercent(int Resource)
        {
            double amount = 0.0;
            double maxAmount = 1.0;
            _vessel.GetConnectedResourceTotals(Resource, true, out amount, out maxAmount);
            if (maxAmount != 0.0)
            {
                return 100.0 * amount / maxAmount;
            }
            else
            {
                return Double.NaN;
            }
        }
        
        // TODO dont work
        public void SetOrientation(float a, float b, float c)
        {
            Vector3 targetOrientation = new Vector3(a,b,c);
            _vessel.Autopilot.SAS.SetTargetOrientation(targetOrientation, true);
        }
    }
}
