using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MiniMacro
{
    public enum MacroState { Idle, Recording, Playing, Paused }

    internal sealed class MacroEngine
    {
        private readonly MacroRecorder _recorder = new MacroRecorder();
        private readonly MacroPlayer   _player   = new MacroPlayer();

        private List<MacroAction> _actions = new List<MacroAction>();

        public MacroState State           { get; private set; } = MacroState.Idle;
        public bool       HasRecording    => _actions.Count > 0;
        public string?    CurrentFilePath { get; private set; }
        public string?    CurrentName     { get; private set; }

        public event Action<MacroState>? StateChanged;

        public MacroEngine()
        {
            _player.PlaybackCompleted += OnPlaybackCompleted;
        }

        // ── Запись ───────────────────────────────────────────────────────────

        public void StartRecording()
        {
            if (State != MacroState.Idle) return;
            _recorder.Start();
            SetState(MacroState.Recording);
        }

        public void StopRecording()
        {
            if (State != MacroState.Recording) return;
            _actions      = _recorder.Stop();
            CurrentName   = null;
            CurrentFilePath = null;
            SetState(MacroState.Idle);
        }

        // ── Воспроизведение ──────────────────────────────────────────────────

        public void StartPlayback()
        {
            if (State != MacroState.Idle || !HasRecording) return;
            SetState(MacroState.Playing);
            _player.Play(_actions,
                SettingsManager.Current.PlaybackSpeed,
                SettingsManager.Current.RepeatCount);
        }

        public void Pause()
        {
            if (State == MacroState.Playing)
            {
                _player.Pause();
                SetState(MacroState.Paused);
            }
            else if (State == MacroState.Paused)
            {
                _player.Resume();
                SetState(MacroState.Playing);
            }
        }

        public void Stop()
        {
            if (State == MacroState.Idle) return;
            if (State == MacroState.Recording)
            {
                _actions = _recorder.Stop();
                SetState(MacroState.Idle);
            }
            else
            {
                _player.Stop();
                // состояние сбрасывается в OnPlaybackCompleted
            }
        }

        private void OnPlaybackCompleted()
        {
            // Вызывается из фонового потока — переходим в Idle
            Application.Current?.Dispatcher.Invoke(() => SetState(MacroState.Idle));
        }

        // ── Файловые операции ────────────────────────────────────────────────

        public void SaveTo(string filePath)
        {
            int w = (int)SystemParameters.PrimaryScreenWidth;
            int h = (int)SystemParameters.PrimaryScreenHeight;
            MacroLibrary.SaveMacro(_actions, w, h, filePath);
            CurrentFilePath = filePath;
            CurrentName     = Path.GetFileNameWithoutExtension(filePath);
        }

        public void LoadFrom(string filePath)
        {
            var json   = File.ReadAllText(filePath);
            var doc    = JsonSerializer.Deserialize<MacroFile>(json,
                             new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                         ?? throw new InvalidDataException("Файл повреждён");

            _actions        = doc.Actions ?? new List<MacroAction>();
            CurrentFilePath = filePath;
            CurrentName     = Path.GetFileNameWithoutExtension(filePath);
        }

        private void SetState(MacroState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        // ── Внутренняя модель JSON ────────────────────────────────────────────

        private class MacroFile
        {
            public int                Version     { get; set; }
            public int                ScreenWidth  { get; set; }
            public int                ScreenHeight { get; set; }
            public List<MacroAction>? Actions     { get; set; }
        }
    }
}
