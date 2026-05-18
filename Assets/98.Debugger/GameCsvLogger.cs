using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class GameCsvLogger : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private bool _enableLogging = true;
    [SerializeField] private string _logDirectoryName = "Logs";
    [SerializeField] private string _filePrefix = "framework_log";

    private readonly ConcurrentQueue<string> _pendingLines = new ConcurrentQueue<string>();
    private readonly AutoResetEvent _flushSignal = new AutoResetEvent(false);

    private Thread _writerThread;
    private volatile bool _isRunning;
    private string _logFilePath;

    private void OnEnable()
    {
        if (!_enableLogging)
        {
            return;
        }

        StartWriter();
        SubscribeEvents();
        Log(GameLogEventType.ApplicationStarted);
    }

    private void OnDisable()
    {
        if (!_enableLogging)
        {
            return;
        }

        Log(GameLogEventType.ApplicationQuit);
        UnsubscribeEvents();
        StopWriter();
    }

    private void StartWriter()
    {
        string executableDirectory = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(executableDirectory))
        {
            Debug.LogError("[GameCsvLogger] Failed to resolve executable directory. Fallback to persistentDataPath.");
            executableDirectory = Application.persistentDataPath;
        }

        string basePath = Path.Combine(executableDirectory, _logDirectoryName);
        Directory.CreateDirectory(basePath);

        string fileName = $"{_filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _logFilePath = Path.Combine(basePath, fileName);

        _isRunning = true;
        _writerThread = new Thread(WriterLoop)
        {
            IsBackground = true,
            Name = "GameCsvLoggerWriter"
        };
        _writerThread.Start();

        EnqueueLine("TimestampUtc,EventType,ActorId,ActorName,TargetId,TargetName,Metadata");
        Debug.Log($"[GameCsvLogger] Logging started: {_logFilePath}");
    }

    private void StopWriter()
    {
        _isRunning = false;
        _flushSignal.Set();

        if (_writerThread != null)
        {
            _writerThread.Join(1000);
            _writerThread = null;
        }

        while (_pendingLines.TryDequeue(out _))
        {
        }

        Debug.Log("[GameCsvLogger] Logging stopped.");
    }

    private void WriterLoop()
    {
        try
        {
            using (var stream = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                while (_isRunning || !_pendingLines.IsEmpty)
                {
                    bool wroteAny = false;
                    while (_pendingLines.TryDequeue(out string line))
                    {
                        writer.WriteLine(line);
                        wroteAny = true;
                    }

                    if (wroteAny)
                    {
                        writer.Flush();
                    }

                    if (_isRunning)
                    {
                        _flushSignal.WaitOne(50);
                    }
                }

                writer.Flush();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameCsvLogger] WriterLoop failed: {ex}");
        }
    }

    private void SubscribeEvents()
    {
        EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Instance.Subscribe<InGameStateChangedEvent>(OnInGameStateChanged);
        EventBus.Instance.Subscribe<SceneLoadRequestedEvent>(OnSceneLoadRequested);
        EventBus.Instance.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
        EventBus.Instance.Subscribe<PauseRequestedEvent>(OnPauseRequested);
        EventBus.Instance.Subscribe<TimeScaleChangedEvent>(OnTimeScaleChanged);
        EventBus.Instance.Subscribe<CameraShakeEvent>(OnCameraShakeRequested);
        EventBus.Instance.Subscribe<PlaySoundEvent>(OnPlaySoundRequested);
        EventBus.Instance.Subscribe<StopSoundEvent>(OnStopSoundRequested);
        EventBus.Instance.Subscribe<MoveInputEvent>(OnMoveInput);
        EventBus.Instance.Subscribe<LookInputEvent>(OnLookInput);
        EventBus.Instance.Subscribe<InputDeviceChangedEvent>(OnInputDeviceChanged);
        EventBus.Instance.Subscribe<SubmitInputEvent>(OnSubmitInput);
        EventBus.Instance.Subscribe<CancelInputEvent>(OnCancelInput);
        EventBus.Instance.Subscribe<PauseInputEvent>(OnPauseInput);
        EventBus.Instance.Subscribe<PrimaryActionInputEvent>(OnPrimaryActionInput);
        EventBus.Instance.Subscribe<SecondaryActionInputEvent>(OnSecondaryActionInput);
        EventBus.Instance.Subscribe<CombatStrikeInputEvent>(OnCombatStrikeInput);
        EventBus.Instance.Subscribe<CombatPierceInputEvent>(OnCombatPierceInput);
        EventBus.Instance.Subscribe<CombatHexInputEvent>(OnCombatHexInput);
        EventBus.Instance.Subscribe<CombatGuardInputEvent>(OnCombatGuardInput);
        EventBus.Instance.Subscribe<CombatEndTurnInputEvent>(OnCombatEndTurnInput);
        EventBus.Instance.Subscribe<HitStopRequestedEvent>(OnHitStopRequested);
        EventBus.Instance.Subscribe<SlowMotionRequestedEvent>(OnSlowMotionRequested);

        EventBus.Instance.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Subscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Subscribe<CombatActionRequestedEvent>(OnCombatActionRequested);
        EventBus.Instance.Subscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Subscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Subscribe<CombatBreakTriggeredEvent>(OnCombatBreakTriggered);
        EventBus.Instance.Subscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Subscribe<CombatMinimumFallAppliedEvent>(OnCombatMinimumFallApplied);
        EventBus.Instance.Subscribe<CombatBonusTurnGrantedEvent>(OnCombatBonusTurnGranted);
        EventBus.Instance.Subscribe<CombatTurnEndedEvent>(OnCombatTurnEnded);
        EventBus.Instance.Subscribe<CombatEndedEvent>(OnCombatEnded);
    }

    private void UnsubscribeEvents()
    {
        EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Instance.Unsubscribe<InGameStateChangedEvent>(OnInGameStateChanged);
        EventBus.Instance.Unsubscribe<SceneLoadRequestedEvent>(OnSceneLoadRequested);
        EventBus.Instance.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
        EventBus.Instance.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
        EventBus.Instance.Unsubscribe<TimeScaleChangedEvent>(OnTimeScaleChanged);
        EventBus.Instance.Unsubscribe<CameraShakeEvent>(OnCameraShakeRequested);
        EventBus.Instance.Unsubscribe<PlaySoundEvent>(OnPlaySoundRequested);
        EventBus.Instance.Unsubscribe<StopSoundEvent>(OnStopSoundRequested);
        EventBus.Instance.Unsubscribe<MoveInputEvent>(OnMoveInput);
        EventBus.Instance.Unsubscribe<LookInputEvent>(OnLookInput);
        EventBus.Instance.Unsubscribe<InputDeviceChangedEvent>(OnInputDeviceChanged);
        EventBus.Instance.Unsubscribe<SubmitInputEvent>(OnSubmitInput);
        EventBus.Instance.Unsubscribe<CancelInputEvent>(OnCancelInput);
        EventBus.Instance.Unsubscribe<PauseInputEvent>(OnPauseInput);
        EventBus.Instance.Unsubscribe<PrimaryActionInputEvent>(OnPrimaryActionInput);
        EventBus.Instance.Unsubscribe<SecondaryActionInputEvent>(OnSecondaryActionInput);
        EventBus.Instance.Unsubscribe<CombatStrikeInputEvent>(OnCombatStrikeInput);
        EventBus.Instance.Unsubscribe<CombatPierceInputEvent>(OnCombatPierceInput);
        EventBus.Instance.Unsubscribe<CombatHexInputEvent>(OnCombatHexInput);
        EventBus.Instance.Unsubscribe<CombatGuardInputEvent>(OnCombatGuardInput);
        EventBus.Instance.Unsubscribe<CombatEndTurnInputEvent>(OnCombatEndTurnInput);
        EventBus.Instance.Unsubscribe<HitStopRequestedEvent>(OnHitStopRequested);
        EventBus.Instance.Unsubscribe<SlowMotionRequestedEvent>(OnSlowMotionRequested);

        EventBus.Instance.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Unsubscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Unsubscribe<CombatActionRequestedEvent>(OnCombatActionRequested);
        EventBus.Instance.Unsubscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Unsubscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Unsubscribe<CombatBreakTriggeredEvent>(OnCombatBreakTriggered);
        EventBus.Instance.Unsubscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Unsubscribe<CombatMinimumFallAppliedEvent>(OnCombatMinimumFallApplied);
        EventBus.Instance.Unsubscribe<CombatBonusTurnGrantedEvent>(OnCombatBonusTurnGranted);
        EventBus.Instance.Unsubscribe<CombatTurnEndedEvent>(OnCombatTurnEnded);
        EventBus.Instance.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        Log(GameLogEventType.GameStateChanged, metadata: new Dictionary<string, object>
        {
            { "previous_state", evt.PreviousState.ToString() },
            { "new_state", evt.NewState.ToString() }
        });
    }

    private void OnInGameStateChanged(InGameStateChangedEvent evt)
    {
        Log(GameLogEventType.InGameStateChanged, metadata: new Dictionary<string, object>
        {
            { "previous_state", evt.PreviousState.ToString() },
            { "new_state", evt.NewState.ToString() }
        });
    }

    private void OnSceneLoadRequested(SceneLoadRequestedEvent evt)
    {
        Log(GameLogEventType.SceneLoadRequested, metadata: new Dictionary<string, object>
        {
            { "scene_name", evt.SceneName ?? string.Empty }
        });
    }

    private void OnSceneLoaded(SceneLoadedEvent evt)
    {
        Log(GameLogEventType.SceneLoaded, metadata: new Dictionary<string, object>
        {
            { "scene_name", evt.SceneName ?? string.Empty }
        });
    }

    private void OnPauseRequested(PauseRequestedEvent evt)
    {
        Log(evt.Pause ? GameLogEventType.PauseRequested : GameLogEventType.ResumeRequested);
    }

    private void OnTimeScaleChanged(TimeScaleChangedEvent evt)
    {
        Log(GameLogEventType.TimeScaleChanged, metadata: new Dictionary<string, object>
        {
            { "time_scale", evt.NewTimeScale }
        });
    }

    private void OnCameraShakeRequested(CameraShakeEvent evt)
    {
        Log(GameLogEventType.CameraShakeRequested, metadata: new Dictionary<string, object>
        {
            { "intensity", evt.Intensity.ToString() }
        });
    }

    private void OnPlaySoundRequested(PlaySoundEvent evt)
    {
        Log(GameLogEventType.SoundRequested, metadata: new Dictionary<string, object>
        {
            { "mode", evt.IsBgm ? "Bgm" : "Sfx" },
            { "volume", evt.Volume },
            { "clip_name", evt.Clip != null ? evt.Clip.name : string.Empty }
        });
    }

    private void OnStopSoundRequested(StopSoundEvent evt)
    {
        Log(GameLogEventType.SoundRequested, metadata: new Dictionary<string, object>
        {
            { "mode", evt.StopBgm ? "StopBgm" : "StopSfx" }
        });
    }

    private void OnMoveInput(MoveInputEvent evt) => LogInput("Move", evt.Value.ToString());
    private void OnLookInput(LookInputEvent evt) => LogInput("Look", evt.Value.ToString());
    private void OnInputDeviceChanged(InputDeviceChangedEvent evt) => LogInput("DeviceChanged", evt.DeviceName ?? string.Empty);
    private void OnSubmitInput(SubmitInputEvent evt) => LogInput("Submit", "Triggered");
    private void OnCancelInput(CancelInputEvent evt) => LogInput("Cancel", "Triggered");
    private void OnPauseInput(PauseInputEvent evt) => LogInput("Pause", "Triggered");
    private void OnPrimaryActionInput(PrimaryActionInputEvent evt) => LogInput("PrimaryAction", evt.IsPressed ? "Pressed" : "Released");
    private void OnSecondaryActionInput(SecondaryActionInputEvent evt) => LogInput("SecondaryAction", evt.IsPressed ? "Pressed" : "Released");
    private void OnCombatStrikeInput(CombatStrikeInputEvent evt) => LogInput("CombatStrike", "Triggered");
    private void OnCombatPierceInput(CombatPierceInputEvent evt) => LogInput("CombatPierce", "Triggered");
    private void OnCombatHexInput(CombatHexInputEvent evt) => LogInput("CombatHex", "Triggered");
    private void OnCombatGuardInput(CombatGuardInputEvent evt) => LogInput("CombatGuard", "Triggered");
    private void OnCombatEndTurnInput(CombatEndTurnInputEvent evt) => LogInput("CombatEndTurn", "Triggered");

    private void OnHitStopRequested(HitStopRequestedEvent evt)
    {
        Log(GameLogEventType.Custom, metadata: new Dictionary<string, object>
        {
            { "event_name", "HitStopRequested" },
            { "duration", evt.Duration },
            { "time_scale", evt.TimeScale }
        });
    }

    private void OnSlowMotionRequested(SlowMotionRequestedEvent evt)
    {
        Log(GameLogEventType.Custom, metadata: new Dictionary<string, object>
        {
            { "event_name", "SlowMotionRequested" },
            { "duration", evt.Duration },
            { "time_scale", evt.TimeScale }
        });
    }

    private void OnCombatStarted(CombatStartedEvent evt) => LogCombatEvent("CombatStartedEvent", evt.Snapshot);
    private void OnCombatTurnStarted(CombatTurnStartedEvent evt) => LogCombatEvent("CombatTurnStartedEvent", evt.Snapshot);
    private void OnCombatActionExecuted(CombatActionExecutedEvent evt) => LogCombatEvent("CombatActionExecutedEvent", evt.Snapshot);
    private void OnCombatActorDamaged(CombatActorDamagedEvent evt) => LogCombatEvent("CombatActorDamagedEvent", evt.Snapshot);
    private void OnCombatBreakTriggered(CombatBreakTriggeredEvent evt) => LogCombatEvent("CombatBreakTriggeredEvent", evt.Snapshot);
    private void OnCombatGroggyApplied(CombatGroggyAppliedEvent evt) => LogCombatEvent("CombatGroggyAppliedEvent", evt.Snapshot);
    private void OnCombatTurnEnded(CombatTurnEndedEvent evt) => LogCombatEvent("CombatTurnEndedEvent", evt.Snapshot);

    private void OnCombatActionRequested(CombatActionRequestedEvent evt)
    {
        Log(GameLogEventType.CombatEvent, metadata: new Dictionary<string, object>
        {
            { "event_name", "CombatActionRequestedEvent" },
            { "action_type", evt.ActionType.ToString() }
        });
    }

    private void OnCombatMinimumFallApplied(CombatMinimumFallAppliedEvent evt)
    {
        Log(GameLogEventType.CombatEvent, metadata: new Dictionary<string, object>
        {
            { "event_name", "CombatMinimumFallAppliedEvent" },
            { "actor", evt.Actor.ToString() },
            { "forced_amount", evt.ForcedAmount },
            { "minimum_fall", evt.MinimumFall }
        });
    }

    private void OnCombatBonusTurnGranted(CombatBonusTurnGrantedEvent evt)
    {
        Dictionary<string, object> metadata = BuildSnapshotMetadata(evt.Snapshot, "CombatBonusTurnGrantedEvent");
        metadata["bonus_actor"] = evt.Actor.ToString();
        Log(GameLogEventType.CombatEvent, metadata: metadata);
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        Dictionary<string, object> metadata = BuildSnapshotMetadata(evt.Snapshot, "CombatEndedEvent");
        metadata["player_won"] = evt.PlayerWon;
        Log(GameLogEventType.CombatEvent, metadata: metadata);
    }

    private void LogInput(string actionName, string value)
    {
        Log(GameLogEventType.InputReceived, metadata: new Dictionary<string, object>
        {
            { "action", actionName },
            { "value", value }
        });
    }

    private void LogCombatEvent(string eventName, CombatLogSnapshot snapshot)
    {
        Log(GameLogEventType.CombatEvent, metadata: BuildSnapshotMetadata(snapshot, eventName));
    }

    private static Dictionary<string, object> BuildSnapshotMetadata(CombatLogSnapshot snapshot, string eventName)
    {
        return new Dictionary<string, object>
        {
            { "event_name", eventName },
            { "turn_index", snapshot.turn_index },
            { "turn_state", snapshot.turn_state.ToString() },
            { "actor", snapshot.actor.ToString() },
            { "action_type", snapshot.action_type.ToString() },
            { "spent_sand", snapshot.spent_sand },
            { "damage", snapshot.damage },
            { "player_hp", snapshot.player_hp },
            { "enemy_hp", snapshot.enemy_hp },
            { "player_available_sand", snapshot.player_available_sand },
            { "enemy_available_sand", snapshot.enemy_available_sand },
            { "player_transferred_sand", snapshot.player_transferred_sand },
            { "enemy_transferred_sand", snapshot.enemy_transferred_sand },
            { "player_guard_value", snapshot.player_guard_value },
            { "enemy_threat", snapshot.enemy_threat },
            { "enemy_guard_value", snapshot.enemy_guard_value },
            { "enemy_groggy_pending", snapshot.enemy_groggy_pending },
            { "enemy_groggy_active", snapshot.enemy_groggy_active }
        };
    }

    public void LogCustom(string message, Dictionary<string, object> metadata = null, GameObject actor = null, GameObject target = null)
    {
        var resolvedMetadata = metadata ?? new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(message))
        {
            resolvedMetadata["message"] = message;
        }

        Log(GameLogEventType.Custom, actor, target, resolvedMetadata);
    }

    public void Log(GameLogEventType eventType, GameObject actor = null, GameObject target = null, Dictionary<string, object> metadata = null)
    {
        EntitySnapshot actorSnapshot = BuildEntitySnapshot(actor);
        EntitySnapshot targetSnapshot = BuildEntitySnapshot(target);

        string line = string.Join(",",
            EscapeCsv(DateTime.UtcNow.ToString("O")),
            EscapeCsv(eventType.ToString()),
            EscapeCsv(actorSnapshot.EntityId),
            EscapeCsv(actorSnapshot.EntityName),
            EscapeCsv(targetSnapshot.EntityId),
            EscapeCsv(targetSnapshot.EntityName),
            EscapeCsv(SerializeMetadata(metadata)));

        EnqueueLine(line);
    }

    private void EnqueueLine(string line)
    {
        _pendingLines.Enqueue(line);
        _flushSignal.Set();
    }

    private static EntitySnapshot BuildEntitySnapshot(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return default;
        }

        LoggableEntity loggableEntity = gameObject.GetComponent<LoggableEntity>();
        if (loggableEntity != null)
        {
            return new EntitySnapshot(loggableEntity.EntityId, loggableEntity.DisplayName);
        }

        return new EntitySnapshot(gameObject.GetInstanceID().ToString(), gameObject.name);
    }

    private static string SerializeMetadata(Dictionary<string, object> metadata)
    {
        if (metadata == null || metadata.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        bool isFirst = true;

        foreach (KeyValuePair<string, object> pair in metadata)
        {
            if (!isFirst)
            {
                builder.Append(';');
            }

            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value);
            isFirst = false;
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        string safe = value ?? string.Empty;
        if (safe.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
        {
            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }

        return safe;
    }

    private readonly struct EntitySnapshot
    {
        public readonly string EntityId;
        public readonly string EntityName;

        public EntitySnapshot(string entityId, string entityName)
        {
            EntityId = entityId ?? string.Empty;
            EntityName = entityName ?? string.Empty;
        }
    }
}
