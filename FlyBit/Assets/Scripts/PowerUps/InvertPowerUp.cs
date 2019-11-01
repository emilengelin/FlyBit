﻿using FlyBit.Controllers;

namespace FlyBit.PowerUps
{

    sealed class InvertPowerUp : PowerUp
    {

        protected override void Activate()
        {
            EffectsController.Singleton.ToggleInvertEffect();
        }

    }

}
