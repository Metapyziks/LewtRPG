﻿using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

using ResourceLib;

using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;
using Lewt.Shared.World;
using Lewt.Shared;
using Lewt.Shared.Magic;

namespace Scripts.Entities
{
    public class Skeleton : Character
    {
        private ulong myNextTurnTime;
        private Spell mySpell;

        public override bool CanCast
        {
            get
            {
                return true;
            }
        }
        public Skeleton()
        {
            SetBoundingBox( -3.5 / 8.0, -0.5, 7.0 / 8.0, 0.5 );

            myNextTurnTime = 0;
        }

        public Skeleton( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            BaseMaxHitPoints = 20;
            FinalMaxHitPoints = 20;

            HitPoints = MaxHitPoints;

            BaseWalkSpeed = 2.0;
            FinalWalkSpeed = 2.0;

            mySpell = Spell.Create(SpellInfo.Get("firebolt"), 0.1);

        }

        protected override void InitializeGraphics()
        {
            Anim = new AnimatedSprite( Res.Get<Texture>( "images_character_skeleton_base" ), 32, 32, 8.0, MapRenderer.CameraScale );
            Anim.UseCentreAsOrigin = true;

            base.InitializeGraphics();
        }

        private void RandomizeTarget()
        {
            StartWalking( ( WalkDirection )( (int) ( Tools.Random() * 4.0 ) + 1 ) );
            myNextTurnTime = Map.TimeTicks + Tools.SecondsToTicks( Tools.Random() * 4.0 + 1 );
        }

        public override void Think( double deltaTime )
        {
            base.Think( deltaTime );

            if ( !IsAlive )
                return;

            if (Tools.Random() < 0.005)
                mySpell.Cast(this, new Vector2d(OriginX, OriginY - 0.5), CastAngle); 

            if ( IsServer && ( Map.TimeTicks >= myNextTurnTime || WalkDirection == WalkDirection.Still ) )
                RandomizeTarget();
        }
    }
}
