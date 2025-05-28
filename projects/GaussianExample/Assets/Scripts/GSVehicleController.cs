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

        private int m_curTrackIndex = 0;

        void Start()
        {
            var trajs = ParseTrajectory(trajectoryFile);
            StartCoroutine(FollowTrajectory(trajs, trackId));
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
            m_curTrackIndex = 0;
            var track = trajs.Where(t => t.trackId == id).ToList();
            track.Sort((a, b) => a.frameId.CompareTo(b.frameId));
            var tc = 0.0f;
            while (m_curTrackIndex < track.Count - 1)
            {
                var point = track[m_curTrackIndex];
                var nextPoint = track[m_curTrackIndex + 1];

                var t = nextPoint.timestamp - point.timestamp;
                var dt = Time.deltaTime * speedFactor;
                tc += dt;
                var pos = Vector3.Lerp(point.Position(), nextPoint.Position(), tc / t);
                transform.position = pos;
                transform.rotation = preRotation * point.Rotation();
                if (tc < t)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                tc = 0.0f;
                m_curTrackIndex++;
            }

            this.gameObject.SetActive(false);

        }

    }

}