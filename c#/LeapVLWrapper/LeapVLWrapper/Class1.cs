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
        #region Enum Typecasts
        private static FingerType ToFingerType(Leap.Finger.FingerType leapFingerType)
        {
            return (FingerType)Enum.Parse(typeof(FingerType), leapFingerType.ToString());
        }

        private static Leap.Finger.FingerType ToLeapFingerType(FingerType fingerType)
        {
            return (Leap.Finger.FingerType)Enum.Parse(typeof(Leap.Finger.FingerType), fingerType.ToString());
        }

        private static FingerJoint ToFingerJoint(Leap.Finger.FingerJoint leapFingerJoint)
        {
            return (FingerJoint)Enum.Parse(typeof(FingerJoint), leapFingerJoint.ToString());
        }

        private static Leap.Finger.FingerJoint ToLeapFingerJoint(FingerJoint fingerJoint)
        {
            return (Leap.Finger.FingerJoint)Enum.Parse(typeof(Leap.Finger.FingerJoint), fingerJoint.ToString());
        }

        private static Leap.Bone.BoneType ToLeapBoneType(BoneType boneType)
        {
            return (Leap.Bone.BoneType)Enum.Parse(typeof(Leap.Bone.BoneType), boneType.ToString());
        }

        private static BoneType ToBoneType(Leap.Bone.BoneType leapBoneType)
        {
            return (BoneType)Enum.Parse(typeof(BoneType), leapBoneType.ToString());
        }

        private static Leap.Gesture.GestureType ToLeapGestureType(GestureType gestureType)
        {
            return (Leap.Gesture.GestureType)Enum.Parse(typeof(Leap.Gesture.GestureType), gestureType.ToString());
        }

        private static GestureType ToGestureType (Leap.Gesture.GestureType leapGestureType)
        {
            return (GestureType)Enum.Parse(typeof(GestureType), leapGestureType.ToString());
        }

        private static Leap.Gesture.GestureState ToLeapGestureState(GestureState gestureState)
        {
            return (Leap.Gesture.GestureState)Enum.Parse(typeof(Leap.Gesture.GestureState), gestureState.ToString());
        }

        private static GestureState ToGestureState(Leap.Gesture.GestureState leapGestureState)
        {
            return (GestureState)Enum.Parse(typeof(GestureState), leapGestureState.ToString());
        }
        #endregion

        #region FINGER methods
        public static Leap.Vector GetJointPosition(Leap.Finger finger, out Leap.Finger fingerOut, FingerJoint fingerJoint)
        {
            fingerOut = finger;
            return finger.JointPosition(ToLeapFingerJoint(fingerJoint));
        }

        public static Leap.Bone GetBone(Leap.Finger finger, out Leap.Finger fingerOut, BoneType type)
        {
            fingerOut = finger;
            return finger.Bone(ToLeapBoneType(type));
        }

        public static void GetBones(Leap.Finger finger, out Leap.Finger fingerOut, out Leap.Bone metacarpal, out Leap.Bone proximal, out Leap.Bone intermediate, out Leap.Bone distal)
        {
            metacarpal = finger.Bone(ToLeapBoneType(BoneType.TYPE_METACARPAL));
            proximal = finger.Bone(ToLeapBoneType(BoneType.TYPE_PROXIMAL));
            intermediate = finger.Bone(ToLeapBoneType(BoneType.TYPE_INTERMEDIATE));
            distal = finger.Bone(ToLeapBoneType(BoneType.TYPE_DISTAL));

            fingerOut = finger;
        }
        
        
        public static FingerType GetFingerType(Leap.Finger finger, out Leap.Finger fingerOut)
        {
            fingerOut = finger;
            return ToFingerType(finger.Type);
        }
        #endregion

        public static BoneType GetBoneType(Leap.Bone bone, out Leap.Bone boneOut)
        {
            boneOut = bone;
            return ToBoneType(bone.Type);
        }

        public static GestureType GetGestureType(Leap.Gesture gesture, out Leap.Gesture gestureOut)
        {
            gestureOut = gesture;
            return ToGestureType(gesture.Type);
        }

        public static GestureState GetGestureState(Leap.Gesture gesture, out Leap.Gesture gestureOut)
        {
            gestureOut = gesture;
            return ToGestureState(gesture.State);
        }

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
        TYPEINVALID = -1,
        TYPE_SWIPE = 1,
        TYPESWIPE = 1,
        TYPE_CIRCLE = 4,
        TYPECIRCLE = 4,
        TYPE_SCREEN_TAP = 5,
        TYPESCREENTAP = 5,
        TYPE_KEY_TAP = 6,
        TYPEKEYTAP = 6
    }

    public enum GestureState
    {
        STATE_INVALID = -1,
        STATEINVALID = -1,
        STATE_START = 1,
        STATESTART = 1,
        STATE_UPDATE = 2,
        STATEUPDATE = 2,
        STATE_STOP = 3,
        STATESTOP = 3
    }
    #endregion
}