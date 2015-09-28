using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeapVLWrapper
{
    public class ListenerWrapper : Leap.Listener
    {
        IObserver<Leap.Frame> observer;

        public ListenerWrapper(IObserver<Leap.Frame> observer)
        {
            this.observer = observer;
        }

        public override void OnFrame(Leap.Controller controller)
        {
            observer.OnNext(controller.Frame());
        }
    }

    public class LeapHelper
    {
        //NOTE: I'm giving all inputs of mutable data (like a Leap.finger in GetJointPosition) also as an output of the method, to avoid timing ambiguity in VL
        #region FINGER methods
        public static Leap.Vector GetJointPosition(Leap.Finger finger, out Leap.Finger fingerOut, FingerJoint fingerJoint)
        {
            fingerOut = finger;
            return finger.JointPosition((Leap.Finger.FingerJoint) fingerJoint );
        }

        public static Leap.Bone GetBone(Leap.Finger finger, out Leap.Finger fingerOut, BoneType type)
        {
            fingerOut = finger;
            return finger.Bone((Leap.Bone.BoneType) type);
        }

        public static void GetBones(Leap.Finger finger, out Leap.Finger fingerOut, out Leap.Bone metacarpal, out Leap.Bone proximal, out Leap.Bone intermediate, out Leap.Bone distal)
        {
            metacarpal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_METACARPAL);
            proximal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_PROXIMAL);
            intermediate = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_INTERMEDIATE);
            distal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_DISTAL);

            fingerOut = finger;
        }
        
        
        public static FingerType GetFingerType(Leap.Finger finger, out Leap.Finger fingerOut)
        {
            fingerOut = finger;
            return (FingerType) finger.Type;
        }
        #endregion

        #region BONE methods
        public static BoneType GetBoneType(Leap.Bone bone, out Leap.Bone boneOut)
        {
            boneOut = bone;
            return (BoneType) bone.Type;
        }
        #endregion

        #region GESTURE methods
        public static GestureType GetGestureType(Leap.Gesture gesture, out Leap.Gesture gestureOut)
        {
            gestureOut = gesture;
            return (GestureType)gesture.Type;
        }

        public static GestureState GetGestureState(Leap.Gesture gesture, out Leap.Gesture gestureOut)
        {
            gestureOut = gesture;
            return (GestureState) gesture.State;
        }

        public static void EnableGesture (Leap.Controller controller, out Leap.Controller controllerOut, GestureType gestureType)
        {
            controller.EnableGesture((Leap.Gesture.GestureType) gestureType);
            controllerOut = controller;
        }

        public static void DisableGesture(Leap.Controller controller, out Leap.Controller controllerOut, GestureType gestureType)
        {
            controller.EnableGesture((Leap.Gesture.GestureType)gestureType, false);
            controllerOut = controller;
        }
        #endregion

        // Workaround: VL is very strict in regards to mutablility: Data linked into delegate regions needs to be immutable.
        // Therefore explicetly pipe data from subscribe to unsubscribe. This data we call "TDataForUnsubscribe"
        public static IObservable<TData> CreateObservable<TDataForSubscribe, TData, TDataForUnsubscribe>(
            TDataForSubscribe dataForSubscribe,
            Func<Tuple<TDataForSubscribe, IObserver<TData>>, TDataForUnsubscribe> onSubscribe,
            Action<TDataForUnsubscribe> onUnsubscribe)
        {
            return
              Observable.Create<TData>((observer) =>
              {
                  var dataForUnsubscribe = onSubscribe(Tuple.Create(dataForSubscribe, observer));
                  return () => { onUnsubscribe(dataForUnsubscribe); };
              });
        }
    }
    #region ENUMS
    public enum FingerType
    {
        TYPE_THUMB = 0,
        TYPE_INDEX = 1,
        TYPE_MIDDLE = 2,
        TYPE_RING = 3,
        TYPE_PINKY = 4
    }

    public enum FingerJoint
    {
        JOINT_MCP = 0,
        JOINT_PIP = 1,
        JOINT_DIP = 2,
        JOINT_TIP = 3
    }

    public enum BoneType
    {
        TYPE_METACARPAL,
        TYPE_PROXIMAL,
        TYPE_INTERMEDIATE,
        TYPE_DISTAL,
    }

    public enum GestureType
    {
        TYPE_INVALID = -1,
        TYPE_SWIPE = 1,
        TYPE_CIRCLE = 4,
        TYPE_SCREEN_TAP = 5,
        TYPE_KEY_TAP = 6,
    }

    public enum GestureState
    {
        STATE_INVALID = -1,
        STATE_START = 1,
        STATE_UPDATE = 2,
        STATE_STOP = 3,
    }
    #endregion
}