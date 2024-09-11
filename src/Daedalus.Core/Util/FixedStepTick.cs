namespace Daedalus.Core;

using Microsoft.Xna.Framework;

public class FixedStepTicker {
    private float _fixedDeltaTime;
    private float _accumulator;
    private GameTime _fixedGameTime;

    public float Interpolant { get; private set; } = 0;

    public FixedStepTicker(float fixedDeltaTime) {
        _fixedDeltaTime = fixedDeltaTime;

        _fixedGameTime = new () { 
            ElapsedGameTime = TimeSpan.FromSeconds(_fixedDeltaTime),
            TotalGameTime = TimeSpan.Zero };
    }

    public void Tick(
        GameTime gameTime, 
        Action<GameTime> update) {

        _fixedGameTime.TotalGameTime = gameTime.TotalGameTime;

        if (_fixedDeltaTime > 0.0) {
            _accumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;

            while (_accumulator >= _fixedDeltaTime) {
                update(_fixedGameTime);

                _accumulator -= _fixedDeltaTime;
            }

            // How close are we to the next fixed update
            //
            // Any system utilizing this for interpoloation will be up 
            // to 1 frame behind the fixed update which is an acceptible 
            // compromize, e.g. renderer (update) vs. physics (fixed update)
            //
            Interpolant = _accumulator / _fixedDeltaTime; 
        }
    }
}