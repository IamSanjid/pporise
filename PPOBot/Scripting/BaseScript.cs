using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PPOBot.Scripting
{
    public abstract class BaseScript
    {
        public string Name { get; protected set; }
        public string Author { get; protected set; }
        public string Description { get; protected set; }

        public event Action<string> ScriptMessage;

        public event Action FlashBotWindow;
        //Asynchronous
        public abstract Task Initialize();
        public virtual void Start() { }
        public virtual void Stop() { }
        public virtual void Pause() { }
        public virtual void Resume() { }
        public virtual void Update() { }
        public virtual void Relog(float seconds, string message, bool autoReconnect) { }

        public virtual void OnBattleMessage(string message) { }
        public virtual void OnSystemMessage(string message) { }
        public virtual void OnLearningMove(string moveName, int pokemonIndex) { }

        public List<Invoker> Invokes = new List<Invoker>();
        public abstract bool ExecuteNextAction();
        protected void LogMessage(string message)
        {
            ScriptMessage?.Invoke(message);
        }

        protected void FlashWindow()
        {
            FlashBotWindow?.Invoke();
        }
    }
}
