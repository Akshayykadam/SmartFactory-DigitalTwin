using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityFactorySceneHDRP
{
	public class CameraMove : MonoBehaviour
	{
		[SerializeField] private CharacterController _characterController;
		[SerializeField] private Transform _playerRoot;
		[SerializeField] private Transform _camera;

		[Space(10)]
		[SerializeField] private float _moveSpeed = 2;
		[SerializeField] private float _rotateSpeed = 2;

		[Space(10)]
		[SerializeField] private float _minWorldY;

		private float _yaw = 0;
		private float _tilt = 0;
		private bool _isRunning = false;
		private bool _isWalkMode = true;

		private void Awake()
		{
			if (_playerRoot != null) _yaw = _playerRoot.eulerAngles.y;
			if (_camera != null) _tilt = _camera.localEulerAngles.x;
		}

		private void Update()
		{
			var mouse = Mouse.current;
			var kb = Keyboard.current;

			// Rotate
			if (mouse != null && mouse.rightButton.isPressed)
			{
				Vector2 mouseDelta = mouse.delta.ReadValue();
				_yaw  += mouseDelta.x * _rotateSpeed * 0.1f;
				_tilt -= mouseDelta.y * _rotateSpeed * 0.1f;

				_tilt = Mathf.Clamp(_tilt, -89, 89);

				if (_playerRoot != null) _playerRoot.eulerAngles = new Vector3(0, _yaw, 0);
				if (_camera != null) _camera.localEulerAngles = new Vector3(_tilt, 0, 0);
			}

			// Move
			float h = 0f;
			float v = 0f;
			if (kb != null)
			{
				if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
				if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
				if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
				if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;

				if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
				{
					_isRunning = !_isRunning;
				}
			}

			Vector3 dir = new Vector3(h, 0, v);
			float vertMove = 0f;
			if (kb != null)
			{
				if (kb.qKey.isPressed) vertMove -= _moveSpeed;
				if (kb.eKey.isPressed) vertMove += _moveSpeed;
			}

			float height = _camera != null ? Mathf.Max(0, _camera.localPosition.y + vertMove * Time.deltaTime) : 0f;

			if (_characterController != null)
			{
				if (_isWalkMode)
				{
					if (_playerRoot != null) dir = Quaternion.Euler(0, _playerRoot.localEulerAngles.y, 0) * dir;
					_characterController.SimpleMove(dir * _moveSpeed * (_isRunning ? 3 : 1));
					if (_camera != null) _camera.localPosition = new Vector3(0, height, 0);
				}
				else
				{
					if (_camera != null && _playerRoot != null)
					{
						dir = Quaternion.Euler(_camera.localEulerAngles.x, _playerRoot.localEulerAngles.y, _camera.localEulerAngles.z) * dir;
					}
					_characterController.Move(dir * _moveSpeed * (_isRunning ? 3 : 1) * Time.deltaTime);
				}
			}

			if (_playerRoot != null && _playerRoot.position.y < _minWorldY)
			{
				Vector3 position = _playerRoot.position;
				position.y = _minWorldY;
				_playerRoot.position = position;
			}

			// Change mode
			if (kb != null && kb.fKey.wasPressedThisFrame)
			{
				_isWalkMode = !_isWalkMode;
				if (_isWalkMode)
				{
					if (_playerRoot != null) _playerRoot.position = new Vector3(_playerRoot.position.x, _minWorldY, _playerRoot.position.z);
					if (_camera != null) _camera.localPosition = new Vector3(0, 1.5f, 0);
				}
				else
				{
					if (_playerRoot != null && _camera != null)
					{
						_playerRoot.position = new Vector3(_playerRoot.position.x, _camera.position.y, _playerRoot.position.z);
						_camera.localPosition = Vector3.zero;
					}
				}
			}
		}
	}
}