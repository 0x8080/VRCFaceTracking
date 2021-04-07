﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRCEyeTracking;
using MelonLoader;
using UnityEngine;
using VRCEyeTracking.SRParam;
using VRCEyeTracking.SRParam.LipMerging;

[assembly: MelonInfo(typeof(MainMod), "VRCEyeTracking", "1.3.0", "benaclejames",
    "https://github.com/benaclejames/VRCEyeTracking")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace VRCEyeTracking
{
    public class MainMod : MelonMod
    {
        public static void ResetParams() => SRanipalTrackParams.ForEach(param => param.ResetParam());
        public static void ZeroParams() => SRanipalTrackParams.ForEach(param => param.ZeroParam());

        private static readonly List<ISRanipalParam> SRanipalTrackParams = new List<ISRanipalParam>
        {
            #region EyeTracking
            
            new SRanipalXYEyeParameter(v2 => Vector3.Scale(
                v2.verbose_data.combined.eye_data.gaze_direction_normalized,
                new Vector3(-1, 1, 1)), "EyesX", "EyesY"),
            
            new SRanipalGeneralEyeParameter(v2 => v2.expression_data.left.eye_wide >
                                                  v2.expression_data.right.eye_wide
                ? v2.expression_data.left.eye_wide
                : v2.expression_data.right.eye_wide, "EyesWiden"),
            
            new SRanipalGeneralEyeParameter(v2 =>
            {
                var normalizedFloat = SRanipalTrack.CurrentDiameter / SRanipalTrack.MinOpen / (SRanipalTrack.MaxOpen - SRanipalTrack.MinOpen);
                return Mathf.Clamp(normalizedFloat, 0, 1);
            }, "EyesDilation"),
            
            new SRanipalXYEyeParameter(v2 => Vector3.Scale(
                v2.verbose_data.left.gaze_direction_normalized,
                new Vector3(-1, 1, 1)), "LeftEyeX", "LeftEyeY"),
            
            new SRanipalXYEyeParameter(v2 => Vector3.Scale(
                v2.verbose_data.right.gaze_direction_normalized,
                new Vector3(-1, 1, 1)), "RightEyeX", "RightEyeY"),
            
            new SRanipalGeneralEyeParameter(v2 => v2.verbose_data.left.eye_openness, "LeftEyeLid", true),
            new SRanipalGeneralEyeParameter(v2 => v2.verbose_data.right.eye_openness, "RightEyeLid", true),
            
            new SRanipalGeneralEyeParameter(v2 => v2.expression_data.left.eye_wide, "LeftEyeWiden"),
            new SRanipalGeneralEyeParameter(v2 => v2.expression_data.right.eye_wide, "RightEyeWiden"),
            
            new SRanipalGeneralEyeParameter(v2 => v2.expression_data.right.eye_squeeze, "LeftEyeSqueeze"),
            new SRanipalGeneralEyeParameter(v2 => v2.expression_data.right.eye_squeeze, "RightEyeSqueeze"),
            
            #endregion
        };
        
        public static void AppendLipParams()
        {
            // Add optimized shapes
            SRanipalTrackParams.AddRange(LipShapeMerger.GetOptimizedLipParameters());
            
            // Add unoptimized shapes in case someone wants to use em
            foreach (var unoptimizedShape in LipShapeMerger.GetUnoptimizedLipShapes())
                SRanipalTrackParams.Add(new SRanipalLipParameter(v2 => v2[unoptimizedShape], 
                    unoptimizedShape.ToString()));
        }

        public override void OnApplicationStart()
        {
            DependencyManager.Init();
        }

        public override void VRChat_OnUiManagerInit()
        {
            SRanipalTrack.Initializer.Start();
            Hooking.SetupHooking();
            MelonCoroutines.Start(UpdateParams());
        }

        public override void OnApplicationQuit()
        {
            SRanipalTrack.Stop();
        }

        public override void OnSceneWasLoaded(int level, string levelName)
        {
            //if (level == -1 && !QuickModeMenu.HasInitMenu)
            //    QuickModeMenu.InitializeMenu();
            
            SRanipalTrack.MinOpen = 999;
            SRanipalTrack.MaxOpen = 0;
        }

        private static IEnumerator UpdateParams()
        {
            for (;;)
            {
                foreach (var param in SRanipalTrackParams.Where(param => param.IsParamValid()))
                    param.RefreshParam(SRanipalTrack.LatestEyeData, SRanipalTrack.LatestLipData);

                yield return new WaitForSeconds(0.01f);
            }
        }
    }
}