using UnityEngine;

namespace ActiveRagdoll
{
    internal static class Utils
    {
        internal static void KnockoutJoint(ConfigurableJoint joint)
        {
            var angularXDrive = joint.angularXDrive;
            var angularYZDrive = joint.angularYZDrive;
            angularXDrive.positionSpring = 0;
            angularYZDrive.positionSpring = 0;
            angularXDrive.positionDamper = 0.1f;
            angularYZDrive.positionDamper = 0.3f;
            joint.angularXDrive = angularXDrive;
            joint.angularYZDrive = angularYZDrive;
        }
        internal static void SetJointStrength(ConfigurableJoint joint, float xStrength, float yzStrength, float damper)
        {
            var angularXDrive = joint.angularXDrive;
            var angularYZDrive = joint.angularYZDrive;
            angularXDrive.positionSpring = xStrength;
            angularYZDrive.positionSpring = yzStrength;
            angularXDrive.positionDamper = damper;
            angularYZDrive.positionDamper = damper;
            joint.angularXDrive = angularXDrive;
            joint.angularYZDrive = angularYZDrive;
        }
        
        
        internal static void SetTargetRotationInternal (SkeletonStruct skeletonStruct, Quaternion targetRotation, Space space)
        {
            skeletonStruct.Joint.targetRotation = GetTargetRotationInternal(skeletonStruct,targetRotation, space);
        }
        internal static Quaternion GetTargetRotationInternal(SkeletonStruct skeletonStruct, Quaternion targetRotation, Space space)
        {
            Quaternion resultRotation = Quaternion.Inverse(skeletonStruct.WorldToJointSpace);
            if (space == Space.World) {
                resultRotation *= skeletonStruct.TargetInitialRotation * Quaternion.Inverse (targetRotation);
            } else {
                resultRotation *= Quaternion.Inverse (targetRotation) * skeletonStruct.TargetInitialRotation;
            }
            resultRotation *= skeletonStruct.WorldToJointSpace;
            return resultRotation;
        }
    }
}