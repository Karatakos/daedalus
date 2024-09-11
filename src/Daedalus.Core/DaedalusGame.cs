namespace Daedalus.Core;

using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;

public abstract class DaedalusGame : Game
{
    private FixedStepTicker fixedTicker;

    public float Interpolant { 
        get { return fixedTicker.Interpolant; }
    }

    public DaedalusGame(int fixedTimeStepInFrames) {
        var fixedDeltaTime = 0f;
        if (fixedTimeStepInFrames > 0)
            fixedDeltaTime = (float)1/fixedTimeStepInFrames;

        fixedTicker = new FixedStepTicker(fixedDeltaTime);
    }

    public DaedalusGame() : this(30) {}

    protected abstract void VariableUpdate(GameTime gameTime);

    protected abstract void FixedUpdate(GameTime gameTime);

    protected override void Draw(GameTime gameTime) {
        base.Draw(gameTime);
    }

    protected override void Initialize() {
        base.Initialize();
    }

    protected override void LoadContent() {
        base.LoadContent();
    }

    sealed protected override void Update(GameTime gameTime) {
        fixedTicker.Tick(gameTime, FixedUpdate);

        VariableUpdate(gameTime);

        base.Update(gameTime);
    }
}
