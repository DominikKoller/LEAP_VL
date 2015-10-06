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
        public static Leap.Frame Deserialize(byte[] data, out String errorMessage, out bool error)
        {
            error = false;
            errorMessage = "";
            Leap.Frame frame = new Leap.Frame();
            try
            {
                frame.Deserialize(data);
            }
            catch(Exception e)
            {
                error = true;
                errorMessage = e.ToString();
            }
            return frame;
        }
        #region FINGER methods
        [Node]
        public static Leap.Bone GetBone(Leap.Finger finger, BoneType type)
        {
            return finger.Bone((Leap.Bone.BoneType) type);
        }

        [Node]
        public static void GetBones(Leap.Finger finger, out Leap.Bone metacarpal, out Leap.Bone proximal, out Leap.Bone intermediate, out Leap.Bone distal)
        {
            metacarpal = finger.Bone((Leap.Bone.BoneType) BoneType.Metacarpal);
            proximal = finger.Bone((Leap.Bone.BoneType) BoneType.Proximal);
            intermediate = finger.Bone((Leap.Bone.BoneType) BoneType.Intermediate);
            distal = finger.Bone((Leap.Bone.BoneType) BoneType.Distal);
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
        public static void ToggleGesture (Leap.Controller controller, out Leap.Controller controllerOut, GestureType gestureType, bool toggle)
        {
            controllerOut = controller;
            controller.EnableGesture((Leap.Gesture.GestureType) gestureType, toggle);
        }
        #endregion

        #region SET / GET POLICIES
        /* Policy setting requests
        A request to change a policy is subject to user approval and a policy can be changed by the user at any time 
        (using the Leap Motion settings dialog). 
        The desired policy flags must be set every time an application runs.
        Policy changes are completed asynchronously and, because they are subject to user approval or system compatibility checks, may not complete successfully. 
        Call Controller::isPolicySet() after a suitable interval to test whether the change was accepted.
        */    

        [Node]
        public static void TogglePolicy(Leap.Controller controller, out Leap.Controller controllerOut, PolicyFlag policy, bool toggle)
        {
            controllerOut = controller;
            if (toggle)
                controller.SetPolicy((Leap.Controller.PolicyFlag) policy);
            else
                controller.ClearPolicy((Leap.Controller.PolicyFlag) policy);
        }

        [Node]
        public static bool IsPolicySet(Leap.Controller controller, out Leap.Controller controllerOut, PolicyFlag policy)
        {
            controllerOut = controller;
            return controller.IsPolicySet((Leap.Controller.PolicyFlag) policy);
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
    #region ENUMS directly from Leap API
    [Type]
    public enum FingerType
    {
        //original, from Leap API
        /*
        TYPE_THUMB = 0,
        TYPE_INDEX = 1,
        TYPE_MIDDLE = 2,
        TYPE_RING = 3,
        TYPE_PINKY = 4
        */
        Thumb = 0,
        Index = 1,
        Middle = 2,
        Ring = 3,
        Pinky = 4
    }

    [Type]
    public enum BoneType
    {
        //original, from Leap API
        /*
        TYPE_METACARPAL,
        TYPE_PROXIMAL,
        TYPE_INTERMEDIATE,
        TYPE_DISTAL,
        */
        Metacarpal,
        Proximal,
        Intermediate,
        Distal
    }

    [Type]
    public enum GestureType
    {
        //original, from Leap API
        /*
        TYPE_INVALID = -1,
        TYPE_SWIPE = 1,
        TYPE_CIRCLE = 4,
        TYPE_SCREEN_TAP = 5,
        TYPE_KEY_TAP = 6,
        */
        Invalid = -1,
        Swipe = 1,
        Circle = 4,
        ScreenTap = 5,
        KeyTap = 6
    }

    [Type]
    public enum GestureState
    {   //original, from Leap API
        /*
        STATE_INVALID = -1,
        STATE_START = 1,
        STATE_UPDATE = 2,
        STATE_STOP = 3,
        */
        Invalid = -1,
        Start = 1,
        Update = 2,
        Stop = 3
    }
    
    [Type]
    public enum PolicyFlag
    {
        //original, from Leap API
        /*
        POLICY_DEFAULT = 0,
        POLICY_BACKGROUND_FRAMES = 1,
        POLICY_IMAGES = 2,
        POLICY_OPTIMIZE_HMD = 4
        */
        Default = 0,
        BackgroundFrames = 1,
        Images = 2,
        OptimizeHMD = 4
    }
    #endregion
    #region own ENUMS
    //only used in 'Selectors' to be able to choose from a list (enum)
    [Type]
    public enum HandSide
    {
        Left,
        Right
    }
    
    [Type]
    public enum PointableType
    {
        Finger,
        Tool
    }

    [Type]
    public enum RelativePosition
    {
        Frontmost,
        Leftmost,
        Rightmost
    }
    #endregion
}
 