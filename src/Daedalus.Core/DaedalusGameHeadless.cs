namespace Daedalus.Core;

using Microsoft.Xna.Framework;

public enum ServerState {
    StartingUp,
    Initializing,
    InLobby,
    InGame,
    ShuttingDown
}

public abstract class DaedalusGameHeadless
{
    private GameTime _gameTime;
    private long _gameStartTick;
    private long _lastTick;
    private FixedStepTicker ticker;

    public float Interpolant { 
        get { return ticker.Interpolant; }
    }

    public DaedalusGameHeadless(int fixedTimeStepInFrames) {
        var fixedDeltaTime = 0f;
        if (fixedTimeStepInFrames > 0)
            fixedDeltaTime = (float)1/fixedTimeStepInFrames;

        ticker = new FixedStepTicker(fixedDeltaTime);
        
        _gameTime = new () { 
            ElapsedGameTime = TimeSpan.Zero,
            TotalGameTime = TimeSpan.Zero };
    }

    public DaedalusGameHeadless() : this(30) {}

    public void Run() {
        Initialize();
        LoadContent();
        StartLoop();
    } 

    public void Exit() {
        // TODO: Cleanup on game exit
    }

    protected abstract void VariableUpdate(GameTime gameTime);

    protected abstract void FixedUpdate(GameTime gameTime);

    protected virtual void Initialize() {}

    protected virtual void LoadContent() {}

    private void StartLoop() {
        _gameStartTick = DateTime.Now.Ticks;
        _lastTick = DateTime.Now.Ticks;
        
        while (true) {
            var now = DateTime.Now.Ticks;
            
            _gameTime.TotalGameTime = TimeSpan.FromTicks(now - _gameStartTick);
            _gameTime.ElapsedGameTime = TimeSpan.FromTicks(now - _lastTick);

            _lastTick = now;

            ticker.Tick(_gameTime, FixedUpdate);

            VariableUpdate(_gameTime);
        }
    } 
}
