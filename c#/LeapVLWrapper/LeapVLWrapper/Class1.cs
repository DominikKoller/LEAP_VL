using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace LeapVLWrapper
{
    // Abstract classes are not supported in VL
    [Type]
    public class ListenerWrapper : Leap.Listener
    {
        IObserver<Leap.Frame> observer;

        [Node]
        public ListenerWrapper(IObserver<Leap.Frame> observer)
        {
            this.observer = observer;
        }

        [Node]
        public override void OnFrame(Leap.Controller controller)
        {
            observer.OnNext(controller.Frame());
        }
    }

    // VL calls Dispose on all fields which are IDisposable. In this case we need to ensure that
    // the controller gets disposed AFTER the subscription of the IConnectableObservable.
    // To tell VL to execute Controller.Dispose before subscription.Dispose is also not possible yet.
    // Therefor (for now) much easier to put this logic here.
    // Workaround: VL is very strict in regards to mutablility: Data linked into delegate regions needs to be immutable.
    // Therefore explicetly pipe data from subscribe to unsubscribe. This data we call "TDataForUnsubscribe"
    [Type]
    [Node(OperationsOfProcessNode = "Create, Controller, Notifications")]
    public class Observable<TController, TData, TDataForUnsubscribe> : IDisposable
        where TController : class
    {
        [Node]
        public TController Controller { get; }

        [Node]
        public IObservable<TData> Notifications { get; }

        IDisposable _subscription;

        [Node]
        public Observable(
            TController controller,
            Func<Tuple<TController, IObserver<TData>>, TDataForUnsubscribe> onSubscribe,
            Action<TDataForUnsubscribe> onUnsubscribe)
        {
            Controller = controller;
            var n =
              Observable.Create<TData>((observer) =>
              {
                  var dataForUnsubscribe = onSubscribe(Tuple.Create(controller, observer));
                  return () => { onUnsubscribe(dataForUnsubscribe); };
              })
              .Publish();
            _subscription = n.Connect();
            Notifications = n;
        }

        [Node]
        public void Dispose()
        {
            _subscription.Dispose();
            var disposable = Controller as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }

    [Type]
    public class LeapHelper
    {
        //we flagged Leap.Frame as an immutable type
        //However, Leap.Frame.Deserialize returns void
        //Therefore it cannot be used like the method of an immutable type, so here's the workaround:
        [Node]
        public static Leap.Frame Deserialize(byte[] data)
        {
            Leap.Frame frame = new Leap.Frame();
            frame.Deserialize(data);
            return frame;
        }

        #region FINGER methods
        [Node]
        public static Leap.Vector GetJointPosition(Leap.Finger finger, FingerJoint fingerJoint)
        {
            return finger.JointPosition((Leap.Finger.FingerJoint) fingerJoint );
        }

        [Node]
        public static Leap.Bone GetBone(Leap.Finger finger, BoneType type)
        {
            return finger.Bone((Leap.Bone.BoneType) type);
        }

        [Node]
        public static void GetBones(Leap.Finger finger, out Leap.Bone metacarpal, out Leap.Bone proximal, out Leap.Bone intermediate, out Leap.Bone distal)
        {
            metacarpal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_METACARPAL);
            proximal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_PROXIMAL);
            intermediate = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_INTERMEDIATE);
            distal = finger.Bone((Leap.Bone.BoneType) BoneType.TYPE_DISTAL);
        }

        [Node]
        public static FingerType GetFingerType(Leap.Finger finger)
        {
            return (FingerType) finger.Type;
        }
        #endregion

        #region BONE methods
        [Node]
        public static BoneType GetBoneType(Leap.Bone bone)
        {
            return (BoneType) bone.Type;
        }
        #endregion

        #region GESTURE methods
        [Node]
        public static GestureType GetGestureType(Leap.Gesture gesture)
        {
            return (GestureType)gesture.Type;
        }

        [Node]
        public static GestureState GetGestureState(Leap.Gesture gesture)
        {
            return (GestureState) gesture.State;
        }

        //NOTE: I'm giving all inputs of mutable data (like Leap.Controller) also as an output of the method, to avoid timing ambiguity in VL
        [Node]
        public static void EnableGesture (Leap.Controller controller, out Leap.Controller controllerOut, GestureType gestureType)
        {
            controller.EnableGesture((Leap.Gesture.GestureType) gestureType);
            controllerOut = controller;
        }

        [Node]
        public static void DisableGesture(Leap.Controller controller, out Leap.Controller controllerOut, GestureType gestureType)
        {
            controller.EnableGesture((Leap.Gesture.GestureType)gestureType, false);
            controllerOut = controller;
        }
        #endregion

        //// Workaround: VL is very strict in regards to mutablility: Data linked into delegate regions needs to be immutable.
        //// Therefore explicetly pipe data from subscribe to unsubscribe. This data we call "TDataForUnsubscribe"
        //[Node]
        //public static IObservable<TData> CreateObservable<TDataForSubscribe, TData, TDataForUnsubscribe>(
        //    TDataForSubscribe dataForSubscribe,
        //    Func<Tuple<TDataForSubscribe, IObserver<TData>>, TDataForUnsubscribe> onSubscribe,
        //    Action<TDataForUnsubscribe> onUnsubscribe)
        //{
        //    return
        //      Observable.Create<TData>((observer) =>
        //      {
        //          var dataForUnsubscribe = onSubscribe(Tuple.Create(dataForSubscribe, observer));
        //          return () => { onUnsubscribe(dataForUnsubscribe); };
        //      });
        //}
    }
    #region ENUMS
    [Type]
    public enum FingerType
    {
        TYPE_THUMB = 0,
        TYPE_INDEX = 1,
        TYPE_MIDDLE = 2,
        TYPE_RING = 3,
        TYPE_PINKY = 4
    }

    [Type]
    public enum FingerJoint
    {
        JOINT_MCP = 0,
        JOINT_PIP = 1,
        JOINT_DIP = 2,
        JOINT_TIP = 3
    }

    [Type]
    public enum BoneType
    {
        TYPE_METACARPAL,
        TYPE_PROXIMAL,
        TYPE_INTERMEDIATE,
        TYPE_DISTAL,
    }

    [Type]
    public enum GestureType
    {
        TYPE_INVALID = -1,
        TYPE_SWIPE = 1,
        TYPE_CIRCLE = 4,
        TYPE_SCREEN_TAP = 5,
        TYPE_KEY_TAP = 6,
    }

    [Type]
    public enum GestureState
    {
        STATE_INVALID = -1,
        STATE_START = 1,
        STATE_UPDATE = 2,
        STATE_STOP = 3,
    }
    #endregion
}