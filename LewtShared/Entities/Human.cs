using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using ResourceLib;

using Lewt.Shared.Rendering;
using Lewt.Shared.Magic;

namespace Lewt.Shared.Entities
{
    public class Human : Character
    {
        public Human()
        {
            SetBoundingBox( -3.5 / 8.0, -0.5, 7.0 / 8.0, 0.5 );

            HitPoints = MaxHitPoints;
            ManaLevel = MaxManaLevel;
        }

        public Human( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
        
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            BaseMaxHitPoints = 10;
            FinalMaxHitPoints = 100;

            BaseMaxManaLevel = 0;
            FinalMaxManaLevel = 500;

            BaseWalkSpeed = 4.0;
            FinalWalkSpeed = 12.0;

            BaseManaRechargePeriod = 1.0 / 4.0;
            FinalManaRechargePeriod = 1.0 / 64.0;

            BaseCastCooldownTime = 1.0;
            FinalCastCooldownTime = 1.0 / 8.0;

            HPRechargeDelay = 5.0;
            SlowHPRechargePeriod = 4.0;
            FastHPRechargePeriod = 1.0 / 16.0;

            BaseFastHPRechargeDelay = 120.0;
            FinalFastHPRechargeDelay = 15.0;
        }

        protected override void InitializeGraphics()
        {
            Anim = new AnimatedSprite( Res.Get<Texture>( "images_character_human_base" ), 32, 32, 8.0, MapRenderer.CameraScale );
            Anim.UseCentreAsOrigin = true;

            base.InitializeGraphics();
        }
    }
}
