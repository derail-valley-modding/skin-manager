﻿using System.Collections.Generic;
using System.Linq;
using DV;
using DV.Customization.Paint;
using DV.ThingTypes;
using DV.UserManagement;
using HarmonyLib;
using SMShared;
using UnityEngine;

#nullable disable
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

        // Sounds
        public AudioClip HoverCarSound;
        public AudioClip SelectedCarSound;
        public AudioClip ConfirmSound;
        public AudioClip CancelSound;

        private State CurrentState;
        private LayerMask TrainCarMask;
        private RaycastHit Hit;
        private TrainCar SelectedCar = null;
        private TrainCar PointedCar = null;
        private bool HasInterior = false;
        private MeshRenderer HighlighterRender;

        private PaintArea AreaToPaint = PaintArea.All;
        private PaintArea AlreadyPainted = PaintArea.None;

        private List<CustomPaintTheme> SkinsForCarType = null;
        private int SelectedSkinIdx = 0;
        private CustomPaintTheme SelectedSkin = null;
        private (string exterior, string interior) CurrentThemeName;

        private const float SIGNAL_RANGE = 100f;
        private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
        private static readonly Color LASER_COLOR = new Color(1f, 0.5f, 0f);
        public Color GetLaserBeamColor()
        {
            return LASER_COLOR;
        }
        public void OverrideSignalOrigin(Transform signalOrigin) => this.signalOrigin = signalOrigin;

        #region Initialization

        public void Awake()
        {
            // steal components from other radio modes
            CommsRadioCarDeleter deleter = Controller.deleteControl;

            if (deleter)
            {
                signalOrigin = deleter.signalOrigin;
                display = deleter.display;
                selectionMaterial = new Material(deleter.selectionMaterial);
                skinningMaterial = new Material(deleter.deleteMaterial);
                trainHighlighter = deleter.trainHighlighter;

                // sounds
                HoverCarSound = deleter.hoverOverCar;
                SelectedCarSound = deleter.warningSound;
                ConfirmSound = deleter.confirmSound;
                CancelSound = deleter.cancelSound;
            }
            else
            {
                Debug.LogError("CommsRadioSkinSwitcher: couldn't get properties from siblings");
            }
        }

        public void Start()
        {
            if (!signalOrigin)
            {
                Debug.LogError("CommsRadioSkinSwitcher: signalOrigin on isn't set, using this.transform!", this);
                signalOrigin = transform;
            }

            if (display == null)
            {
                Debug.LogError("CommsRadioSkinSwitcher: display not set, can't function properly!", this);
            }

            if ((selectionMaterial == null) || (skinningMaterial == null))
            {
                Debug.LogError("CommsRadioSkinSwitcher: Selection material(s) not set. Visuals won't be correct.", this);
            }

            if (trainHighlighter == null)
            {
                Debug.LogError("CommsRadioSkinSwitcher: trainHighlighter not set, can't function properly!!", this);
            }

            if ((HoverCarSound == null) || (SelectedCarSound == null) || (ConfirmSound == null) || (CancelSound == null))
            {
                Debug.LogError("Not all audio clips set, some sounds won't be played!", this);
            }

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
            display.SetDisplay(Translations.ReskinMode, Translations.SelectCarPrompt, string.Empty);
        }

        #endregion

        #region Car Highlighting

        private void HighlightCar(TrainCar car, Material highlightMaterial)
        {
            if (car == null)
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

        private void PointToCar(TrainCar car)
        {
            if (PointedCar != car)
            {
                if (car != null)
                {
                    PointedCar = car;
                    HighlightCar(PointedCar, selectionMaterial);
                    CommsRadioController.PlayAudioFromRadio(HoverCarSound, transform);
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

        private void SetState(State newState)
        {
            if (newState == CurrentState) return;

            CurrentState = newState;
            switch (CurrentState)
            {
                case State.SelectCar:
                    SetStartingDisplay();
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    break;

                case State.SelectSkin:
                    UpdateAvailableSkinsList(SelectedCar.carLivery);
                    SetSelectedSkin(SkinsForCarType?.FirstOrDefault());
                    CurrentThemeName = SkinManager.GetCurrentCarSkin(SelectedCar, false);

                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;

                case State.SelectAreas:
                    AreaToPaint = PaintArea.All;
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

            switch (CurrentState)
            {
                case State.SelectCar:
                    if (!(SelectedCar == null))
                    {
                        Debug.LogError("Invalid setup for current state, reseting flags!", this);
                        ResetState();
                        return;
                    }

                    // Check if not pointing at anything
                    if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask))
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
                    if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) &&
                        (trainCar = TrainCar.Resolve(Hit.transform.root)) && (trainCar == SelectedCar))
                    {
                        PointToCar(trainCar);

                        if (!HasInterior && !SkinProvider.IsBuiltInTheme(SelectedSkin) && (SelectedSkin.name == CurrentThemeName.exterior))
                        {
                            display.SetAction(Translations.ReloadAction);
                        }
                        else
                        {
                            display.SetAction(Translations.SelectAction);
                        }
                    }
                    else
                    {
                        PointToCar(null);
                        display.SetAction(Translations.CancelAction);
                    }

                    break;

                case State.SelectAreas:
                    display.SetContent($"{Translations.SelectAreasPrompt}\n{AreaToPaintName}");

                    if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) &&
                        (trainCar = TrainCar.Resolve(Hit.transform.root)) && (trainCar == SelectedCar))
                    {
                        PointToCar(trainCar);

                        if ((AlreadyPainted == AreaToPaint) && !SkinProvider.IsBuiltInTheme(SelectedSkin))
                        {
                            display.SetAction(Translations.ReloadAction);
                        }
                        else
                        {
                            display.SetAction(Translations.ConfirmAction);
                        }
                    }
                    else
                    {
                        PointToCar(null);
                        display.SetAction(Translations.CancelAction);
                    }
                    break;

                default:
                    ResetState();
                    break;
            }
        }

        private string AreaToPaintName
        {
            get
            {
                return AreaToPaint switch
                {
                    PaintArea.Exterior => CommsRadioLocalization.MODE_PAINTJOB_EXTERIOR,
                    PaintArea.Interior => CommsRadioLocalization.MODE_PAINTJOB_INTERIOR,
                    _ => CommsRadioLocalization.MODE_PAINTJOB_ALL,
                };
                ;
            }
        }

        public void OnUse()
        {
            switch (CurrentState)
            {
                case State.SelectCar:
                    if (PointedCar != null)
                    {
                        SelectedCar = PointedCar;
                        HasInterior = SelectedCar.GetComponents<TrainCarPaint>().Any(tcp => tcp.TargetArea == TrainCarPaint.Target.Interior);
                        PointedCar = null;

                        HighlightCar(SelectedCar, skinningMaterial);
                        CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
                        SetState(State.SelectSkin);
                    }
                    break;

                case State.SelectSkin:
                    if ((PointedCar != null) && (PointedCar == SelectedCar))
                    {
                        if (HasInterior)
                        {
                            SetState(State.SelectAreas);
                        }
                        else
                        {
                            // for regular cars, skip area selection
                            if (!SkinProvider.IsBuiltInTheme(SelectedSkin) && (SelectedSkin.name == CurrentThemeName.exterior))
                            {
                                ReloadAndPrepareApplySelectedSkin();
                            }

                            ApplySelectedSkin();
                            ResetState();
                        }
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    }
                    else
                    {
                        // clicked off the selected car, this means cancel
                        CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                        ResetState();
                    }
                    break;

                case State.SelectAreas:
                    if ((PointedCar != null) && (PointedCar == SelectedCar))
                    {
                        // clicked on the selected car again, this means confirm
                        if ((AlreadyPainted == AreaToPaint) && !SkinProvider.IsBuiltInTheme(SelectedSkin))
                        {
                            ReloadAndPrepareApplySelectedSkin();
                        }

                        ApplySelectedSkin();
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    }

                    ResetState();
                    break;
            }
        }

        public bool ButtonACustomAction()
        {
            if (CurrentState == State.SelectSkin)
            {
                if ((SkinsForCarType == null) || (SkinsForCarType.Count == 0)) return false;

                SelectedSkinIdx -= 1;
                if (SelectedSkinIdx < 0) SelectedSkinIdx = SkinsForCarType.Count - 1;

                var selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else if (CurrentState == State.SelectAreas)
            {
                AreaToPaint -= 1;
                if (AreaToPaint == 0) AreaToPaint = PaintArea.All;
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
            if (CurrentState == State.SelectSkin)
            {
                if ((SkinsForCarType == null) || (SkinsForCarType.Count == 0)) return false;

                SelectedSkinIdx += 1;
                if (SelectedSkinIdx >= SkinsForCarType.Count) SelectedSkinIdx = 0;

                var selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else if (CurrentState == State.SelectAreas)
            {
                AreaToPaint += 1;
                if (AreaToPaint > PaintArea.All) AreaToPaint = PaintArea.Exterior;
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

        private void UpdateAvailableSkinsList(TrainCarLivery carType)
        {
            SkinsForCarType = SkinProvider.GetSkinsForType(carType);
            SelectedSkinIdx = 0;
        }

        private void ApplySelectedSkin()
        {
            if (SelectedSkin == null)
            {
                Debug.LogWarning("Tried to reskin to null selection");
            }

            SkinManager.ApplySkin(SelectedCar, SelectedSkin, AreaToPaint);
            //CurrentThemeName = SelectedSkin.name;

            if (SelectedCar.rearCoupler.IsCoupled() && (SelectedCar.rearCoupler.coupledTo?.train is TrainCar attachedCar))
            {
                if (CarTypes.IsMUSteamLocomotive(SelectedCar.carType) && CarTypes.IsTender(attachedCar.carLivery))
                {
                    // car attached behind loco is tender
                    if (SelectedSkin.SupportsVehicle(attachedCar.carLivery))
                    {
                        // found a matching skin for the tender :D
                        SkinManager.ApplySkin(attachedCar, SelectedSkin, AreaToPaint);
                    }
                }
                else if ((SelectedCar.carLivery.id == Constants.DE6_LIVERY_ID) && CarTypes.IsSlug(attachedCar.carLivery))
                {
                    // car attached behind DE6 is slug
                    if (SelectedSkin.SupportsVehicle(attachedCar.carLivery))
                    {
                        SkinManager.ApplySkin(attachedCar, SelectedSkin, AreaToPaint);
                    }
                }
            }
        }

        private void ReloadAndPrepareApplySelectedSkin()
        {
            // Reload the skin.
            SkinProvider.ReloadSkin(SelectedCar.carLivery.id, SelectedSkin.name);
            // Store the original skin's name.
            var tempSkin = SelectedSkin.assetName;
            // Apply the default skin as a middle step to force it to update.
            SelectedSkin = SkinProvider.GetBaseTheme(SMShared.Json.BaseTheme.DVRT);
            ApplySelectedSkin();
            // Refresh the current skins to use the updated version.
            // No need to reset the index as no skins were added/removed.
            SkinsForCarType = SkinProvider.GetSkinsForType(SelectedCar.carLivery);
            SelectedSkin = SkinsForCarType.First(x => x.name == tempSkin);
        }

        private void SetSelectedSkin(CustomPaintTheme theme)
        {
            if (theme is null)
            {
                // should never actually reach this code due to built in themes
                SelectedSkin = null;
                display.SetContent("No Available Themes!");
            }
            else
            {
                SelectedSkin = theme;

                AlreadyPainted = PaintArea.None;
                if (CurrentThemeName.exterior == SelectedSkin.name)
                {
                    AlreadyPainted |= PaintArea.Exterior;
                }
                if (CurrentThemeName.interior == SelectedSkin.name)
                {
                    AlreadyPainted |= PaintArea.Interior;
                }

                string displayName = $"{Translations.SelectPaintPrompt}\n{SelectedSkin.LocalizedName}";
                display.SetContent(displayName);
            }
        }

        #endregion

        protected enum State
        {
            SelectCar,
            SelectSkin,
            SelectAreas,
        }
    }
    
    [HarmonyPatch(typeof(CommsRadioController))]
    internal static class CommsRadio_Awake_Patch
    {
        public static CommsRadioSkinSwitcher SkinSwitcher = null;

        [HarmonyPatch(nameof(CommsRadioController.Awake))]
        [HarmonyPostfix]
        private static void AfterAwake(CommsRadioController __instance, List<ICommsRadioMode> ___allModes)
        {
            CommsRadioSkinSwitcher.Controller = __instance;
            SkinSwitcher = __instance.gameObject.AddComponent<CommsRadioSkinSwitcher>();

            int paintModeIndex = ___allModes.IndexOf(__instance.carPaintjobControl);
            ___allModes[paintModeIndex] = SkinSwitcher;
        }

        [HarmonyPatch(nameof(CommsRadioController.UpdateModesAvailability))]
        [HarmonyPostfix]
        private static void AfterUpdateModesAvailability(CommsRadioController __instance)
        {
            var gameParams = Globals.G.GameParams;
            int skinModeIdx = __instance.allModes.IndexOf(SkinSwitcher);

            bool isSandbox = UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam");
            bool paintingAllowed = (gameParams.CommsRadioSandboxCheatMode && isSandbox) || gameParams.CommsRadioCheatMode;

            if (paintingAllowed || Main.Settings.alwaysAllowRadioReskin)
            {
                __instance.disabledModeIndices.Remove(skinModeIdx);
            }
            else
            {
                __instance.disabledModeIndices.Add(skinModeIdx);
                if (__instance.activeModeIndex == skinModeIdx)
                {
                    __instance.SetNextMode();
                }
            }
        }
    }
}
