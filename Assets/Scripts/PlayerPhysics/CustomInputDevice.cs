using UnityEngine;
using UnityEngine.XR;

namespace CustomPhysics
{
    //Why did I make this class? I don't know. Don't ask such hard questions.
    public class CustomInputDevice
    {
        InputDevice inputDevice;
        public CustomInputDevice(XRNode node)
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(node);
        }

        public void SetDevice(XRNode node)
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(node);
        }

        //Should these just be variables?
        public bool IsValid() => inputDevice.isValid;
        public Vector3 GetPosition() => inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var position) ? position : Vector3.zero;
        public Vector3 GetVelocity() => inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var velocity) ? velocity : Vector3.zero;
        public Quaternion GetRotation() => inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation) ? rotation : Quaternion.identity;
        public bool GetGrip() => inputDevice.TryGetFeatureValue(CommonUsages.grip, out var grip) && grip > 0;
    }
}