﻿/*
 * Copyright (C) 2012 Interactive Lab
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
 * 
 */

using System.Collections.Generic;
using TouchScript.Clusters;
using UnityEngine;

namespace TouchScript.Gestures {
    /// <summary>
    /// Flick gesture.
    /// Recognizes fast movement before releasing touches.
    /// Doesn't care how much time touch points were on surface and how much they moved.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Flick Gesture")]
    public class FlickGesture : Gesture {
        #region Unity fields

        /// <summary>
        /// Time interval in seconds in which points cluster must move by <see cref="MinDistance"/>.
        /// </summary>
        public float FlickTime = .5f;

        /// <summary>
        /// Minimum distance in cm to move in <see cref="FlickTime"/> before ending gesture for it to be recognized.
        /// </summary>
        public float MinDistance = 1f;

        /// <summary>
        /// Minimum distance in cm for cluster to move to be considered as a possible gesture. 
        /// Prevents misinterpreting taps.
        /// </summary>
        public float MovementThreshold = 0.5f;

        /// <summary>
        /// If true, tracks only horizontal movement
        /// </summary>
        public bool Horizontal = false;

        /// <summary>
        /// If true, tracks only vertical movement
        /// </summary>
        public bool Vertical = false;

        #endregion

        #region Private variables

        private Cluster1 cluster = new Cluster1();
        private bool moving = false;
        private Vector2 movementBuffer = Vector2.zero;
        private List<Vector2> positionDeltas = new List<Vector2>();
        private List<float> timeDeltas = new List<float>();
        private float previousTime;

        #endregion

        #region Public properties

        /// <summary>
        /// Contains flick direction (not normalized) when gesture is recognized.
        /// </summary>
        public Vector2 ScreenFlickVector { get; private set; }

        #endregion

        #region Gesture callbacks

        protected override void touchesBegan(IList<TouchPoint> touches) {
            foreach (var touch in touches) {
                if (cluster.AddPoint(touch) == Cluster.OperationResult.FirstPointAdded) {
                    previousTime = Time.time;
                }
            }
        }

        protected override void touchesMoved(IList<TouchPoint> touches) {
            cluster.Invalidate();
            var delta = cluster.GetCenterPosition() - cluster.GetPreviousCenterPosition();
            if (!moving) {
                movementBuffer += delta;
                var dpiMovementThreshold = MovementThreshold*Manager.DotsPerCentimeter;
                if (movementBuffer.sqrMagnitude > dpiMovementThreshold*dpiMovementThreshold) {
                    moving = true;
                }
            }

            positionDeltas.Add(cluster.GetCenterPosition() - cluster.GetPreviousCenterPosition());
            timeDeltas.Add(Time.time - previousTime);
            previousTime = Time.time;
        }

        protected override void touchesEnded(IList<TouchPoint> touches) {
            foreach (var touch in touches) {
                var result = cluster.RemovePoint(touch);
                if (result == Cluster.OperationResult.LastPointRemoved) {
                    if (!moving) {
                        setState(GestureState.Failed);
                        return;
                    }

                    var totalTime = 0f;
                    var totalMovement = Vector2.zero;
                    var i = timeDeltas.Count - 1;
                    while (i >= 0 && totalTime < FlickTime) {
                        if (totalTime + timeDeltas[i] < FlickTime) {
                            totalTime += timeDeltas[i];
                            totalMovement += positionDeltas[i];
                            i--;
                        } else {
                            break;
                        }
                    }

                    if (Horizontal) totalMovement.y = 0;
                    if (Vertical) totalMovement.x = 0;

                    var distance = totalMovement.magnitude;

                    if (distance < MinDistance*TouchManager.Instance.DotsPerCentimeter) {
                        setState(GestureState.Failed);
                    } else {
                        ScreenFlickVector = totalMovement;
                        setState(GestureState.Recognized);
                    }
                }
            }
        }

        protected override void touchesCancelled(IList<TouchPoint> touches) {
            touchesEnded(touches);
        }

        protected override void reset() {
            cluster.RemoveAllPoints();
            moving = false;
            movementBuffer = Vector2.zero;
            timeDeltas.Clear();
            positionDeltas.Clear();
        }

        #endregion
    }
}