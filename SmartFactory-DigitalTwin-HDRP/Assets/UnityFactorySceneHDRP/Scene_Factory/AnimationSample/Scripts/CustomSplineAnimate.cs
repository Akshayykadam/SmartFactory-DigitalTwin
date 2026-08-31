using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace UnityFactorySceneHDRP
{
	[ExecuteAlways]
	public class CustomSplineAnimate : MonoBehaviour
	{
		public static bool IsEmergencyPaused = false;

		[System.Serializable]
		private struct StopPoint
		{
			public float time;
			public float duration;
			public Animation robotArmAnimation;
		}

		[SerializeField] private SplineContainer _spline;
		[SerializeField] private float _duration;
		[SerializeField] private float _startOffset;
		[SerializeField] private StopPoint[] _stopPoints;

		[Header("Preview")]
		[SerializeField, Range(0, 1)] private float _previewTime;

		private Transform _transform;
		private float _time = 0;

		private void Awake()
		{
			if(Application.isPlaying)
			{
				_transform = transform;
				_time += _startOffset;
			}
		}

		private void Start()
		{
			if(Application.isPlaying)
			{
				if(_spline != null && _duration > 0)
				{
					StartCoroutine(Animate());
				}
			}
		}

		private IEnumerator Animate()
		{
			bool[] isPassed = new bool[_stopPoints != null ? _stopPoints.Length : 0];
			float stopOverrun = 0;

			if (_stopPoints != null)
			{
				for(int i = 0; i < _stopPoints.Length; i++)
				{
					if(_time > _stopPoints[i].time && !isPassed[i])
					{
						isPassed[i] = true;
					}
				}
			}

			while(true)
			{ 
				// Emergency Stop Pause Loop
				if (IsEmergencyPaused)
				{
					yield return null;
					continue;
				}

				if (_stopPoints != null)
				{
					for(int i = 0; i < _stopPoints.Length; i++)
					{
						if(_time > _stopPoints[i].time && !isPassed[i])
						{
							isPassed[i] = true;
							stopOverrun = _time - _stopPoints[i].time;
							if (_stopPoints[i].robotArmAnimation != null)
							{
								_stopPoints[i].robotArmAnimation.Play();
							}
							_time = _stopPoints[i].time;
							SetPositionAndRotation(_time);

							float timer = 0f;
							while (timer < _stopPoints[i].duration)
							{
								if (!IsEmergencyPaused)
								{
									timer += Time.deltaTime;
								}
								yield return null;
							}
						}
					}
				}

				_time = _time + stopOverrun + Time.deltaTime / _duration;
				stopOverrun = 0;
				if(_time > 1)
				{
					_time %= 1;
					if (isPassed != null)
					{
						for(int i = 0; i < isPassed.Length; i++)
						{
							isPassed[i] = false;
						}
					}
				}

				SetPositionAndRotation(_time);

				yield return null;
			}
		}

		private void SetPositionAndRotation(float time)
		{
			if (_spline == null) return;

			Vector3 position = _spline.EvaluatePosition(time);
			float3  tangent = _spline.EvaluateTangent(time);

			if (_transform != null)
			{
				_transform.position = position;
				if (math.lengthsq(tangent) > 0.0001f)
				{
					_transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
				}
			}
		}

		#if UNITY_EDITOR
		private void Update()
		{
			if(!Application.isPlaying && _spline != null)
			{
				if(_transform == null)
				{
					_transform = transform;
				}
				SetPositionAndRotation(_previewTime);
			}
		}
		#endif
	}
}