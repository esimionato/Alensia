﻿using Alensia.Core.Camera;
using Alensia.Core.Common;
using NUnit.Framework;
using UnityEngine;
using TestRange = NUnit.Framework.RangeAttribute;

namespace Alensia.Tests.Camera
{
    [TestFixture, Description("Test suite for BaseOrbitingCamera class.")]
    public class BaseOrbitingCameraTest : BaseCameraTest<BaseOrbitingCamera>
    {
        public Transform Pivot
        {
            get { return Camera.Pivot; }
        }

        public float ActualDistance
        {
            get { return Vector3.Distance(Camera.Transform.position, Pivot.position); }
        }

        public float ActualHeading
        {
            get
            {
                var offset = (Pivot.position - Camera.Transform.position).normalized;
                var direction = Vector3.ProjectOnPlane(offset, Pivot.up);

                var heading = Vector3.Angle(Pivot.forward, direction);
                var cross = Pivot.InverseTransformDirection(Vector3.Cross(Pivot.forward, direction));

                if (cross.y < 0) heading = -heading;

                return GeometryUtils.NormalizeAspectAngle(heading);
            }
        }

        public float ActualElevation
        {
            get
            {
                var offset = (Pivot.position - Camera.Transform.position).normalized;
                var direction = Quaternion.AngleAxis(-ActualHeading, Pivot.up) * offset;

                var elevation = Vector3.Angle(Pivot.forward, direction);
                var cross = Pivot.InverseTransformDirection(Vector3.Cross(Pivot.forward, direction));

                if (cross.x > 0) elevation = -elevation;

                return GeometryUtils.NormalizeAspectAngle(elevation);
            }
        }

        [TearDown]
        public override void TearDown()
        {
            if (Pivot != null)
            {
                Object.Destroy(Pivot.gameObject);
            }

            base.TearDown();
        }

        protected override BaseOrbitingCamera CreateCamera(UnityEngine.Camera camera)
        {
            var pivotObject = new GameObject();
            var pivot = pivotObject.GetComponent<Transform>();

            return new TestCamera(pivot, camera);
        }

        [Test, Description("Changing Heading/Elevation/Distance should move the camera to a proper position.")]
        public void ShouldMoveCameraToProperPosition(
            [Values(5, 10)] float distance,
            [TestRange(-180, 180, 45)] float heading,
            [TestRange(-80, 80, 40)] float elevation)
        {
            Camera.Heading = heading;
            Camera.Elevation = elevation;
            Camera.Distance = distance;

            var aspectAngle = GeometryUtils.NormalizeAspectAngle(heading);

            Expect(
                ActualHeading,
                Is.EqualTo(aspectAngle).Within(Tolerance),
                "Unexpected camera heading.");
            Expect(
                ActualElevation,
                Is.EqualTo(elevation).Within(Tolerance),
                "Unexpected camera elevation.");
            Expect(
                ActualDistance,
                Is.EqualTo(distance).Within(Tolerance),
                "Unexpected camera distance.");
        }

        [Test, Description("It should adjust the camera position according to the anchor's position and rotation.")]
        public void ShouldReflectAnchorPositionAndRotation(
            [Values(1, 5)] float distance,
            [Values(-120, 60)] float heading,
            [Values(-40, 15)] float elevation)
        {
            Pivot.eulerAngles = new Vector3
            {
                x = Random.Range(-180, 180),
                y = Random.Range(-180, 180),
                z = Random.Range(-180, 180)
            };

            Pivot.position = new Vector3
            {
                x = Random.Range(-10, 10),
                y = Random.Range(-10, 10),
                z = Random.Range(-10, 10)
            };

            Camera.Heading = heading;
            Camera.Elevation = elevation;
            Camera.Distance = distance;

            Expect(
                ActualHeading,
                Is.EqualTo(heading).Within(Tolerance),
                "Unexpected camera heading.");
            Expect(
                ActualElevation,
                Is.EqualTo(elevation).Within(Tolerance),
                "Unexpected camera elevation.");
            Expect(
                ActualDistance,
                Is.EqualTo(distance).Within(Tolerance),
                "Unexpected camera distance.");
        }

        [Test, Description("The camera should follow the anchor's position and rotation per every tick.")]
        public void ShouldFollowAnchorPositionAndRotationPerEveryTick(
            [Values(1, 5)] float distance,
            [Values(-120, 60)] float heading,
            [Values(-40, 15)] float elevation)
        {
            Camera.Heading = heading;
            Camera.Elevation = elevation;
            Camera.Distance = distance;

            Pivot.eulerAngles = new Vector3
            {
                x = Random.Range(-180, 180),
                y = Random.Range(-180, 180),
                z = Random.Range(-180, 180)
            };

            Pivot.position = new Vector3
            {
                x = Random.Range(-10, 10),
                y = Random.Range(-10, 10),
                z = Random.Range(-10, 10)
            };

            Camera.LateTick();

            Expect(
                ActualHeading,
                Is.EqualTo(heading).Within(Tolerance),
                "Unexpected camera heading.");
            Expect(
                ActualElevation,
                Is.EqualTo(elevation).Within(Tolerance),
                "Unexpected camera elevation.");
            Expect(
                ActualDistance,
                Is.EqualTo(distance).Within(Tolerance),
                "Unexpected camera distance.");
        }

        [Test, Description("Heading property should be clamped between the min. and the max. values.")]
        public void ShouldClampHeadingProperty()
        {
            Camera.RotationalConstraints.Side = 60;

            Camera.Heading = -80;

            Expect(
                Camera.Heading,
                Is.EqualTo(-60).Within(Tolerance),
                "Unexpected camera heading.");

            Camera.Heading = 80;

            Expect(
                Camera.Heading,
                Is.EqualTo(60).Within(Tolerance),
                "Unexpected camera heading.");
        }

        [Test, Description("Elevation property should be clamped between the min. and the max. values.")]
        public void ShouldClampElevationProperty()
        {
            Camera.RotationalConstraints.Up = 60;
            Camera.RotationalConstraints.Down = 30;

            Camera.Elevation = 80;

            Expect(
                Camera.Elevation,
                Is.EqualTo(60).Within(Tolerance),
                "Unexpected camera elevation.");

            Camera.Elevation = -50;

            Expect(
                Camera.Elevation,
                Is.EqualTo(-30).Within(Tolerance),
                "Unexpected camera elevation.");
        }

        [Test, Description("Distance property should be clamped between the min. and the max. values.")]
        public void ShouldClampDistanceProperty()
        {
            Camera.Distance = Camera.DistanceSettings.Minimum - 1;

            Expect(
                Camera.Distance,
                Is.EqualTo(Camera.DistanceSettings.Minimum).Within(Tolerance),
                "Unexpected camera distance.");

            Camera.Distance = Camera.DistanceSettings.Maximum + 1;

            Expect(
                Camera.Distance,
                Is.EqualTo(Camera.DistanceSettings.Maximum).Within(Tolerance),
                "Unexpected camera distance.");
        }

        private class TestCamera : BaseOrbitingCamera
        {
            private readonly Transform _pivot;

            private readonly RotationalConstraints _rotationalConstraints;

            private readonly DistanceSettings _distanceSettings;

            public override RotationalConstraints RotationalConstraints
            {
                get { return _rotationalConstraints; }
            }

            public override DistanceSettings DistanceSettings
            {
                get { return _distanceSettings; }
            }

            public override Transform Pivot
            {
                get { return _pivot; }
            }

            protected override Vector3 AxisForward
            {
                get { return _pivot.forward; }
            }

            protected override Vector3 AxisUp
            {
                get { return _pivot.up; }
            }

            public TestCamera(Transform anchor, UnityEngine.Camera camera) : base(camera)
            {
                _pivot = anchor;

                _rotationalConstraints = new RotationalConstraints
                {
                    Up = 90,
                    Down = 90,
                    Side = 180
                };

                _distanceSettings = new DistanceSettings();
            }
        }
    }
}