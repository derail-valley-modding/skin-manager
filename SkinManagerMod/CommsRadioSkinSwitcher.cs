using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DV;
using Harmony12;
using UnityEngine;

namespace SkinManagerMod
{
    public class CommsRadioSkinSwitcher : MonoBehaviour, ICommsRadioMode
    {
        public static CommsRadioController Controller;
        
        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        public CommsRadioDisplay display;
        public Transform signalOrigin;
        public Material selectionMaterial;
        public Material skinningMaterial;
        public GameObject trainHighlighter;

        private State CurrentState;
        private LayerMask TrainCarMask;
        private RaycastHit Hit;
        private TrainCar SelectedCar = null;
        private TrainCar PointedCar = null;
        private MeshRenderer HighlighterRender;

        private List<Skin> SkinsForCarType = null;
        private int SelectedSkinIdx = 0;
        private Skin SelectedSkin = null;

        private const float SIGNAL_RANGE = 100f;
        private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
        private static readonly Color LASER_COLOR = new Color(1f, 0.5f, 0f);
        public Color GetLaserBeamColor()
        {
            return LASER_COLOR;
        }
        public void OverrideSignalOrigin( Transform signalOrigin ) => this.signalOrigin = signalOrigin;

        #region Initialization

        public void Awake()
        {
            // steal components from other radio modes
            CommsRadioCarDeleter deleter = Controller.deleteControl;

            if( deleter != null )
            {
                signalOrigin = deleter.signalOrigin;
                display = deleter.display;
                selectionMaterial = new Material(deleter.selectionMaterial);
                skinningMaterial = new Material(deleter.deleteMaterial);
                trainHighlighter = deleter.trainHighlighter;
            }
            else
            {
                Debug.LogError("CommsRadioSkinSwitcher: couldn't get properties from siblings");
            }
        }

        public void Start()
        {
            if( !signalOrigin )
            {
                Debug.LogError("CommsRadioSkinSwitcher: signalOrigin on isn't set, using this.transform!", this);
                signalOrigin = transform;
            }

            if( display == null )
            {
                Debug.LogError("CommsRadioSkinSwitcher: display not set, can't function properly!", this);
            }

            if( (selectionMaterial == null) || (skinningMaterial == null) )
            {
                Debug.LogError("CommsRadioSkinSwitcher: Selection material(s) not set. Visuals won't be correct.", this);
            }

            if( trainHighlighter == null )
            {
                Debug.LogError("CommsRadioSkinSwitcher: trainHighlighter not set, can't function properly!!", this);
            }

            //if( this.hoverOverCar == null || this.selectedCarSound == null || this.confirmSound == null || this.cancelSound == null || this.warningSound == null || this.removeCarSound == null || this.moneyRemovedSound == null )
            //{
            //    Debug.LogError("Not all audio clips set, some sounds won't be played!", this);
            //}

            TrainCarMask = LayerMask.GetMask(new string[]
            {
                "Train_Big_Collider"
            });

            HighlighterRender = trainHighlighter.GetComponentInChildren<MeshRenderer>(true);
            trainHighlighter.SetActive(false);
            trainHighlighter.transform.SetParent(null);
        }

        public void Enable() { }

        public void Disable()
        {
            ResetState();
        }

        public void SetStartingDisplay()
        {
            string content = "Aim at the vehicle you wish to reskin.";
            display.SetDisplay("RESKIN", content, "");
        }

        #endregion

        #region Car Highlighting

        private void HighlightCar( TrainCar car, Material highlightMaterial )
        {
            if( car == null )
            {
                Debug.LogError("Highlight car is null. Ignoring request.");
                return;
            }
            
            HighlighterRender.material = highlightMaterial;

            trainHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
            Vector3 b = car.transform.up * (trainHighlighter.transform.localScale.y / 2f);
            Vector3 b2 = car.transform.forward * car.Bounds.center.z;
            Vector3 position = car.transform.position + b + b2;

            trainHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
            trainHighlighter.SetActive(true);
            trainHighlighter.transform.SetParent(car.transform, true);
        }

        private void ClearHighlightedCar()
        {
            trainHighlighter.SetActive(false);
            trainHighlighter.transform.SetParent(null);
        }

        private void PointToCar( TrainCar car )
        {
            if( PointedCar != car )
            {
                if( car != null )
                {
                    PointedCar = car;
                    HighlightCar(PointedCar, selectionMaterial);
                    //CommsRadioController.PlayAudioFromRadio(this.hoverOverCar, base.transform);
                }
                else
                {
                    PointedCar = null;
                    ClearHighlightedCar();
                }
            }
        }

        #endregion

        #region State Machine Actions

        private void SetState( State newState )
        {
            if( newState == CurrentState ) return;

            CurrentState = newState;
            switch( CurrentState )
            {
                case State.SelectCar:
                    SetStartingDisplay();
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    break;

                case State.SelectSkin:
                    UpdateAvailableSkinsList(SelectedCar.carType);
                    SetSelectedSkin(SkinsForCarType.FirstOrDefault());

                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;
            }
        }

        private void ResetState()
        {
            PointedCar = null;

            SelectedCar = null;
            ClearHighlightedCar();

            SetState(State.SelectCar);
        }

        public void OnUpdate()
        {
            TrainCar trainCar;

            switch( CurrentState )
            {
                case State.SelectCar:
                    if( !(SelectedCar == null) )
                    {
                        Debug.LogError("Invalid setup for current state, reseting flags!", this);
                        ResetState();
                        return;
                    }

                    // Check if not pointing at anything
                    if( !Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) )
                    {
                        PointToCar(null);
                    }
                    else
                    {
                        // Try to get the traincar we're pointing at
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        PointToCar(trainCar);
                    }

                    break;

                case State.SelectSkin:
                    if( !Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) )
                    {
                        PointToCar(null);
                        display.SetAction("cancel");
                    }
                    else
                    {
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        PointToCar(trainCar);
                        display.SetAction("confirm");
                    }

                    break;

                default:
                    ResetState();
                    break;
            }
        }

        public void OnUse()
        {
            switch( CurrentState )
            {
                case State.SelectCar:
                    if( PointedCar != null )
                    {
                        SelectedCar = PointedCar;
                        PointedCar = null;

                        HighlightCar(SelectedCar, skinningMaterial);
                        SetState(State.SelectSkin);
                    }
                    break;

                case State.SelectSkin:
                    if( (PointedCar != null) && (PointedCar == SelectedCar) )
                    {
                        // clicked on the selected car again, this means confirm
                        ApplySelectedSkin();
                    }

                    ResetState();
                    break;
            }
        }

        public bool ButtonACustomAction()
        {
            if( CurrentState == State.SelectSkin )
            {
                if( (SkinsForCarType == null) || (SkinsForCarType.Count == 0) ) return false;

                SelectedSkinIdx -= 1;
                if( SelectedSkinIdx < 0 ) SelectedSkinIdx = SkinsForCarType.Count - 1;

                Skin selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else
            {
                Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                return false;
            }
        }

        public bool ButtonBCustomAction()
        {
            if( CurrentState == State.SelectSkin )
            {
                if( (SkinsForCarType == null) || (SkinsForCarType.Count == 0) ) return false;

                SelectedSkinIdx += 1;
                if( SelectedSkinIdx >= SkinsForCarType.Count ) SelectedSkinIdx = 0;

                Skin selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else
            {
                Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                return false;
            }
        }

        #endregion

        #region Skin Shenanigans

        private void UpdateAvailableSkinsList( TrainCarType carType )
        {
            if( Main.skinGroups.TryGetValue(carType, out SkinGroup skinGroup) )
            {
                SkinsForCarType = skinGroup.skins;
            }
            else
            {
                SkinsForCarType = null;
            }

            SelectedSkinIdx = 0;
        }

        private void ApplySelectedSkin()
        {
            if( SelectedSkin == null )
            {
                Debug.LogWarning("Tried to reskin to null selection");
            }

            Main.trainCarState[SelectedCar.CarGUID] = SelectedSkin.name;
            Main.ReplaceTexture(SelectedCar);

            if( CarTypes.IsSteamLocomotive(SelectedCar.carType) && SelectedCar.rearCoupler.IsCoupled() )
            {
                TrainCar attachedCar = SelectedCar.rearCoupler.coupledTo?.train;
                if( (attachedCar != null) && CarTypes.IsTender(attachedCar.carType) )
                {
                    // car attached behind loco is tender
                    if( Main.skinGroups.TryGetValue(attachedCar.carType, out SkinGroup tenderGroup) )
                    {
                        if( tenderGroup.skins.Find(s => string.Equals(s.name, SelectedSkin.name)) is Skin tenderSkin )
                        {
                            // found a matching skin for the tender :D
                            Main.trainCarState[attachedCar.CarGUID] = tenderSkin.name;
                            Main.ReplaceTexture(attachedCar);
                        }
                    }
                }
            }
        }

        private void SetSelectedSkin( Skin skin )
        {
            if( skin == null )
            {
                SelectedSkin = null;
                display.SetContent("No available skins!");
            }
            else
            {
                SelectedSkin = skin;
                string displayName = "Select Skin:\n" + skin.name.Replace('_', ' ');
                display.SetContent(displayName);
            }
        }

        #endregion

        protected enum State
        {
            SelectCar,
            SelectSkin,
        }
    }

    [HarmonyPatch(typeof(CommsRadioController), "Awake")]
    static class CommsRadio_Awake_Patch
    {
        public static CommsRadioSkinSwitcher skinSwitcher = null;

        static void Postfix( CommsRadioController __instance, List<ICommsRadioMode> ___allModes )
        {
            CommsRadioSkinSwitcher.Controller = __instance;
            skinSwitcher = __instance.gameObject.AddComponent<CommsRadioSkinSwitcher>();
            ___allModes.Add(skinSwitcher);
        }
    }
}
