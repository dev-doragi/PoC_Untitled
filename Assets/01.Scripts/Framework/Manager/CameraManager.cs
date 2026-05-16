using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-72)]
public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void OnEnable()
    {
        EnsureReferences();
        EventBus.Instance.Subscribe<CameraShakeEvent>(OnCameraShake);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<CameraShakeEvent>(OnCameraShake);
    }

    private void OnCameraShake(CameraShakeEvent evt)
    {
        if (_impulseSource == null)
        {
            EnsureReferences();
        }

        if (_impulseSource == null)
        {
            Debug.LogError("[CameraManager] Cannot shake camera. Impulse source is missing.", this);
            return;
        }

        float amplitude = evt.Intensity switch
        {
            ShakeIntensity.Weak => 0.3f,
            ShakeIntensity.Medium => 0.6f,
            ShakeIntensity.Strong => 1f,
            _ => 0.3f
        };

        _impulseSource.GenerateImpulse(amplitude);
    }

    public void ShakeWeak() => EventBus.Instance.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Weak });
    public void ShakeMedium() => EventBus.Instance.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Medium });
    public void ShakeStrong() => EventBus.Instance.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Strong });

    private void EnsureReferences()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            Debug.LogError("[CameraManager] Main camera reference is missing.", this);
        }

        if (_cinemachineCamera == null)
        {
            _cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        if (_cinemachineCamera == null)
        {
            Debug.LogError("[CameraManager] CinemachineCamera reference is missing.", this);
        }

        if (_impulseSource == null)
        {
            _impulseSource = FindAnyObjectByType<CinemachineImpulseSource>();
        }

        if (_impulseSource == null)
        {
            Debug.LogError("[CameraManager] CinemachineImpulseSource reference is missing.", this);
        }
    }
}
