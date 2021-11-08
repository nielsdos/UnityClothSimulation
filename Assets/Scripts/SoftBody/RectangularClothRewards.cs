using RigidBody;
using UnityEngine;

namespace SoftBody
{
    public sealed partial class RectangularCloth
    {
        /// <summary>
        /// Get distance reward for both grab cloth models based on hand position and target cloth particle index.
        /// </summary>
        /// <param name="leftHandPos">Left hand position</param>
        /// <param name="rightHandPos">Right hand position</param>
        /// <param name="leftIndex">Target cloth particle index of left hand to go to</param>
        /// <param name="rightIndex">Target cloth particle index of right hand to go to</param>
        /// <returns>The distance reward</returns>
        private float GetDistanceRewardGrabCloth(Vector3 leftHandPos, Vector3 rightHandPos, int leftIndex,
            int rightIndex)
        {
            var topLeftCorner = Bones[leftIndex].position;
            var topRightCorner = Bones[rightIndex].position;

            var distanceLeft = Vector3.Distance(leftHandPos, topLeftCorner);
            var distanceRight = Vector3.Distance(rightHandPos, topRightCorner);

            return -(distanceLeft + distanceRight);
        }

        /// <summary>
        /// The reward function for the initial grabbing of the cloth.
        /// </summary>
        /// <param name="leftHandPos">The position of the left grabber</param>
        /// <param name="rightHandPos">The position of the right grabber</param>
        /// <returns>The reward</returns>
        public float GetRewardGrabCloth1(Vector3 leftHandPos, Vector3 rightHandPos)
        {
            var pointsX = GetPointsXAxis();
            var pointsZ = GetPointsZAxis();
            return GetDistanceRewardGrabCloth(leftHandPos, rightHandPos, CoordToIndex(0, pointsZ - 1),
                CoordToIndex(pointsX - 1, pointsZ - 1));
        }

        /// <summary>
        /// The reward function for the grabbing of the folded cloth.
        /// </summary>
        /// <param name="leftHand">The left grabber</param>
        /// <param name="rightHand">The right grabber</param>
        /// <returns>The reward</returns>
        public float GetRewardGrabCloth2(BaxterHandGrab leftHand, BaxterHandGrab rightHand)
        {
            var pointsZ = GetPointsZAxis();
            var distanceReward = GetDistanceRewardGrabCloth(leftHand.transform.position, rightHand.transform.position,
                CoordToIndex(0, pointsZ - 1), CoordToIndex(0, pointsZ / 2));

            var bonus = 0f;
            if (leftHand.DoesGrabberContainOneOfIndices(
                CoordToIndex(0, 0),
                CoordToIndex(0, 1),
                CoordToIndex(0, 2),
                CoordToIndex(1, 0),
                CoordToIndex(1, 1),
                CoordToIndex(1, 2),
                CoordToIndex(2, 0),
                CoordToIndex(2, 1),
                CoordToIndex(2, 2)))
                bonus += 50f;
            if (rightHand.DoesGrabberContainOneOfIndices(
                CoordToIndex(0, pointsZ / 2 - 1),
                CoordToIndex(0, pointsZ / 2 - 2),
                CoordToIndex(0, pointsZ / 2 - 3),
                CoordToIndex(1, pointsZ / 2 - 1),
                CoordToIndex(1, pointsZ / 2 - 2),
                CoordToIndex(1, pointsZ / 2 - 3),
                CoordToIndex(2, pointsZ / 2 - 1),
                CoordToIndex(2, pointsZ / 2 - 2),
                CoordToIndex(2, pointsZ / 2 - 3)
                ))
                bonus += 50f;

            return distanceReward + bonus;
        }

        /// <summary>
        /// The reward function for folding the cloth.
        /// </summary>
        /// <returns>The reward</returns>
        public float GetRewardFold1()
        {
            var pointsX = GetPointsXAxis();
            var pointsZ = GetPointsZAxis();

            var isEven = 0;
            if (pointsZ % 2 == 0) isEven = 1;

            var totDists = 0f;
            for (var x = 0; x < pointsZ; x++)
            for (var z = 1 - isEven; z < pointsX / 2; z++)
            {
                var v1 = Bones[CoordToIndex(x, z)].position;
                var v2 = Bones[CoordToIndex(x, pointsZ - z - 1)].position;
                totDists += Vector3.Distance(v1, v2);
            }

            // Normalize w.r.t. amount of particles & radius of particles.
            totDists /= pointsX * (pointsZ / 2) * amountOfDivisionsInMesh;

            var cornerDists = GetCornerDists();

            const float totDistsMultiplier = 500f;
            const float cornerDistanceMultiplier = 50f;

            //Debug.Log("totDists: " + (totDists * totDistsMultiplier));
            //Debug.Log("cornerDists: " + (cornerDists * cornerDistanceMultiplier));

            // - minimizing totDists will minimize the distance of the points that need to be on top of each other when folded
            // - minimizing cornerDists will minimize stretching the cloth or crimping the cloth so the 
            // cloth will be flat and stretched as much as expected.
            return -(totDists * totDistsMultiplier + cornerDists * cornerDistanceMultiplier);
        }

        /// <summary>
        /// The reward function for folding the cloth a second time.
        /// </summary>
        /// <returns>The reward</returns>
        public float GetRewardFold2()
        {
            var pointsX = GetPointsXAxis();
            var pointsZ = GetPointsZAxis();

            var isEven = 0;
            if (pointsZ % 2 == 0) isEven = 1;

            var totDists = 0f;
            for (var z = 0; z < pointsZ / 2; z++)
            for (var x = 1 - isEven; x < pointsX / 2; x++)
            {
                var v1 = Bones[CoordToIndex(x, z)].position;
                var v2 = Bones[CoordToIndex(pointsX - x - 1, z)].position;
                var v3 = Bones[CoordToIndex(x, pointsZ - z - 1)].position;
                var v4 = Bones[CoordToIndex(pointsX - x - 1, pointsZ - z - 1)].position;
                totDists += Vector3.Distance(v1, v2);
                totDists += Vector3.Distance(v3, v4);
            }

            // Normalize w.r.t. amount of particles & radius of particles.
            totDists /= pointsX * (pointsZ / 2) * amountOfDivisionsInMesh;

            var cornerDists = GetCornerDistsFold2();

            const float totDistsMultiplier = 500f;
            const float cornerDistanceMultiplier = 50f;

            // - minimizing totDists will minimize the distance of the points that need to be on top of each other when folded
            // - minimizing cornerDists*CornerDistanceMultiplier will minimize stretching the cloth or crimping the cloth so the 
            // cloth will be flat and stretched as much as expected. 
            return -(totDists * totDistsMultiplier + cornerDists * cornerDistanceMultiplier);
        }

        /// <summary>
        /// Check if a fold1 episode is below a certain threshold.
        /// </summary>
        /// <param name="threshold">The threshold for a successful episode</param>
        /// <returns>Boolean indicating a successful episode</returns>
        public bool CheckEndEpisodeFold1(float threshold = 0.8f)
        {
            var cornerDists = GetCornerDists();
            return cornerDists > threshold;
        }

        /// <summary>
        /// Check if a fold2 episode is below a certain threshold.
        /// </summary>
        /// <param name="threshold">The threshold for a successful episode</param>
        /// <returns>Boolean indicating a successful episode</returns>
        public bool CheckEndEpisodeFold2(float threshold = 0.8f)
        {
            var cornerDists = GetCornerDistsFold2();
            return cornerDists > threshold;
        }

        /// <summary>
        /// Calculate the corner distances from the cloth corners to each other.
        /// This function only works for an unfolded cloth
        /// </summary>
        /// <returns>Sum of the cloth corner distances</returns>
        private float GetCornerDists()
        {
            var halfWidthLength = width / 2f;
            var pointsX = GetPointsXAxis();
            var pointsY = GetPointsZAxis();

            // Names of the positions are from the perspective of Baxter in the testing scene.
            var topLeftCorner = Bones[CoordToIndex(0, pointsY - 1)].position;
            var middleLeftCorner = Bones[CoordToIndex(0, pointsY / 2)].position;
            var bottomLeftCorner = Bones[CoordToIndex(0, 0)].position;

            var topRightCorner = Bones[CoordToIndex(pointsX - 1, pointsY - 1)].position;
            var middleRightCorner = Bones[CoordToIndex(pointsX - 1, pointsY / 2)].position;
            var bottomRightCorner = Bones[CoordToIndex(pointsX - 1, 0)].position;

            //Debug.Log("top: " + Mathf.Abs(widthLength - Vector3.Distance(topLeftCorner, topRightCorner)));
            //Debug.Log("middle: " + Mathf.Abs(widthLength - Vector3.Distance(middleLeftCorner, middleRightCorner)));
            //Debug.Log("bottom: " + Mathf.Abs(widthLength - Vector3.Distance(bottomLeftCorner, bottomRightCorner)));
            //Debug.Log("leftTop: " + Mathf.Abs(halfWidthLength - Vector3.Distance(topLeftCorner, middleLeftCorner)));
            //Debug.Log("leftBottom: " + Mathf.Abs(halfWidthLength - Vector3.Distance(middleLeftCorner, bottomLeftCorner)));
            //Debug.Log("rightTop: " + Mathf.Abs(halfWidthLength - Vector3.Distance(topRightCorner, middleRightCorner)));
            //Debug.Log("rightBottom: " + Mathf.Abs(halfWidthLength - Vector3.Distance(middleRightCorner, bottomRightCorner)));

            var sum = Mathf.Abs(width - Vector3.Distance(topLeftCorner, topRightCorner))
                      + Mathf.Abs(width - Vector3.Distance(middleLeftCorner, middleRightCorner))
                      + Mathf.Abs(width - Vector3.Distance(bottomLeftCorner, bottomRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(topLeftCorner, middleLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middleLeftCorner, bottomLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(topRightCorner, middleRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middleRightCorner, bottomRightCorner));

            // Average (since each of of these is equally important) & normalize w.r.t. wanted distances.
            return sum / (halfWidthLength * 4f + width * 3f);
        }

        /// <summary>
        /// Calculate the corner distances from the cloth corners to each other.
        /// This function only works when the cloth is already folded once.
        /// </summary>
        /// <returns>Sum of the cloth corner distances</returns>
        private float GetCornerDistsFold2()
        {
            var halfWidthLength = width / 2f;
            var pointsX = GetPointsXAxis();
            var pointsZ = GetPointsZAxis();

            // Names of the positions are from the perspective of Baxter in the testing scene.
            var topLeftCorner = Bones[CoordToIndex(0, pointsZ - 1)].position;
            var topMiddleCorner = Bones[CoordToIndex(pointsX / 2, pointsZ - 1)].position;
            var topRightCorner = Bones[CoordToIndex(pointsX - 1, pointsZ - 1)].position;

            var bottomLeftCorner = Bones[CoordToIndex(0, 0)].position;
            var bottomMiddleCorner = Bones[CoordToIndex(pointsX / 2, 0)].position;
            var bottomRightCorner = Bones[CoordToIndex(pointsX - 1, 0)].position;

            var leftMiddleCorner = Bones[CoordToIndex(0, pointsZ / 2)].position;
            var rightMiddleCorner = Bones[CoordToIndex(pointsX - 1, pointsZ / 2)].position;

            var middle = Bones[CoordToIndex(pointsX / 2, pointsZ / 2)].position;

            var sum = Mathf.Abs(halfWidthLength - Vector3.Distance(topMiddleCorner, topLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(topMiddleCorner, topRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(bottomMiddleCorner, bottomLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(bottomMiddleCorner, bottomRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(leftMiddleCorner, topLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(leftMiddleCorner, bottomLeftCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(rightMiddleCorner, topRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(rightMiddleCorner, bottomRightCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middle, topMiddleCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middle, bottomMiddleCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middle, leftMiddleCorner))
                      + Mathf.Abs(halfWidthLength - Vector3.Distance(middle, rightMiddleCorner));

            // Average (since each of of these is equally important) & normalize w.r.t. wanted distances.
            return sum / (halfWidthLength * 12f);
        }
    }
}