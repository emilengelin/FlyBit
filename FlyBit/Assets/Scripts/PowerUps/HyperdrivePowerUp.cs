﻿using FlyBit.Controllers;

namespace FlyBit.PowerUps
{

    class HyperdrivePowerUp : PowerUp
    {

        protected override void Activate()
        {
            ScoreController.Singleton.AddStatRecordValue(ScoreController.StatRecordType.HyperdrivePowerUpsTaken, 1);

            EffectsController.Singleton.HyperdriveTravel();
        }

    }

}
