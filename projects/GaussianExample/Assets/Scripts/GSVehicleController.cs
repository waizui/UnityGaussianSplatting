using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StreetGS
{

    class GSVehicleController : MonoBehaviour
    {
        class TrajectoryPoint
        {
            private float[] data; //[track_id,frame_id, x, y, z, qw, qx, qy, qz, delta_time]

            public int trackId => (int)data[0];

            public int frameId => (int)data[1];
            public float timestamp => data[9] * 0.5f;

            public TrajectoryPoint(float[] data)
            {
                this.data = data;
            }

            public Vector3 Position()
            {
                return new Vector3(data[2], data[3], data[4]);
            }

            public Quaternion Rotation()
            {
                return new Quaternion(data[5], data[6], data[7], data[8]);
            }
        }

        public TextAsset trajectoryFile;
        public int trackId = 0;
        public Quaternion preRotation;
        public float speedFactor;

        /// <summary>
        /// Current normalized time in the trajectory.
        /// </summary>
        public float currentTime => (m_currentTime + m_curSegTime) / m_totalTime;

        public float totalTime => m_totalTime;

        /// <summary>
        /// Total time of the trajectory for the current track.
        /// </summary>
        private float m_totalTime = 0.0f;

        private float m_currentTime = 0.0f;

        private float m_curSegTime = 0.0f; // current time in current segment

        private int m_curTrackIndex = 0;

        void Start()
        {
            var trajs = ParseTrajectory(trajectoryFile);
            StartCoroutine(FollowTrajectory(trajs, trackId));
        }

        public float GetDeltaTime(int id)
        {
            var trajs = ParseTrajectory(trajectoryFile);
            var track = trajs.Where(t => t.trackId == id).ToList();
            if (track.Count < 2)
                return 0.0f;
            track.Sort((a, b) => a.frameId.CompareTo(b.frameId));
            return track[1].timestamp - track[0].timestamp;
        }

        private List<TrajectoryPoint> ParseTrajectory(TextAsset file)
        {
            var trajs = new List<TrajectoryPoint>();
            var lines = file.text.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                var values = line.Split(',');
                var data = values.Select(float.Parse).ToArray();
                var point = new TrajectoryPoint(data);
                trajs.Add(point);
            }

            return trajs;
        }

        private IEnumerator FollowTrajectory(IEnumerable<TrajectoryPoint> trajs, int id)
        {
            var track = trajs.Where(t => t.trackId == id).ToList();
            track.Sort((a, b) => a.frameId.CompareTo(b.frameId));

            m_curTrackIndex = 0;
            m_currentTime = 0.0f;
            m_totalTime = track.Last().timestamp - track.First().timestamp;

            m_curSegTime = 0.0f; // current time in current segment
            while (m_curTrackIndex < track.Count - 1)
            {
                var point = track[m_curTrackIndex];
                var nextPoint = track[m_curTrackIndex + 1];

                var segmentTime = nextPoint.timestamp - point.timestamp;
                var dt = Time.deltaTime * speedFactor;
                m_curSegTime += dt;
                var pos = Vector3.Lerp(point.Position(), nextPoint.Position(), m_curSegTime / segmentTime);
                transform.position = pos;
                transform.rotation = preRotation * point.Rotation();
                if (m_curSegTime < segmentTime)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                m_curSegTime = 0.0f;
                m_currentTime += segmentTime;
                m_curTrackIndex++;
            }

            this.gameObject.SetActive(false);

        }

    }

}