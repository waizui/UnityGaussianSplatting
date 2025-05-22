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
            private float[] data; //[obj_id, track_id, x, y, z, qw, qx, qy, qz]

            public int trackId => (int)data[1];

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
                var values = line.Split(',');
                if (values.Length == 9)
                {
                    var data = values.Select(float.Parse).ToArray();
                    var point = new TrajectoryPoint(data);
                    trajs.Add(point);
                }
            }

            return trajs;
        }

        private IEnumerator FollowTrajectory(IEnumerable<TrajectoryPoint> trajs, int id)
        {
            m_curTrackIndex = 0;
            var track = trajs.Where(t => t.trackId == id).ToList();
            while (m_curTrackIndex < track.Count)
            {
                var point = track[m_curTrackIndex];
                Debug.Log(point.Position());
                transform.position = point.Position();
                // transform.rotation = Quaternion.AngleAxis(90f, Vector3.left) * point.Rotation();
                transform.rotation = point.Rotation();

                yield return new WaitForSeconds(0.5f);
                m_curTrackIndex++;
            }

        }

    }

}