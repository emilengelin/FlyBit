﻿using FlyBit.Extensions;
using FlyBit.Map;
using FlyBit.Templates;
using FlyBit.UI;
using System.Collections;
using TMPro;
using UnityEngine;

#pragma warning disable 0649

namespace FlyBit.Controllers
{

    class EffectsController : Controller<EffectsController>
    {

        #region Editor

        [Header("References - Invert Colors")]
        [SerializeField] private Camera         mainCamera;
        [SerializeField] private FollowAndTrack cameraFollowAndTrack;
        [SerializeField] private SpriteRenderer playerSpriteRenderer;
        [SerializeField] private UIColorInvert  gameScreen;
        [SerializeField] private UIColorInvert  deathScreen;
        [SerializeField] private UIColorInvert  resetGameButton;
        [SerializeField] private UIColorInvert  uiGainedScoreColorInvert;

        [Space]
        [SerializeField] private TMP_FontAsset textFontAsset;

        [Header("References - Hyperdrive")]
        [SerializeField] private Transform    playerTransform;
        [SerializeField] private Animator     playerAnimator;
        [SerializeField] private Collider2D   playerCollider;
        [SerializeField] private UISpeedlines hyperdriveSpeedlines;

        [Space]
        [SerializeField] private HyperdriveWallSection hyperdriveWallSection;

        [Space]
        [SerializeField] private SectionTemplate hyperdriveEntrySectionTemplate;

        [Header("Values - Hyperdrive")]
        [SerializeField] private float hyperdriveAnimationTime = 1f;
        [SerializeField] private float hyperdriveTravelTime    = 1f;
        [SerializeField] private float hyperdriveWaitTime      = 0.25f;

        [Space]
        [SerializeField] private float hyperdriveMinDistance = 250f;
        [SerializeField] private float hyperdriveMaxDistance = 500f;

        #endregion

        #region Private variables

        private bool invertEffectIsOn;

        #endregion

        public override void OnAwake()
        {
            ResetAllEffects();
        }

        public void ResetAllEffects()
        {
            invertEffectIsOn = true;
            ToggleInvertEffect();
        }

        public void ToggleInvertEffect()
        {
            invertEffectIsOn = !invertEffectIsOn;

            mainCamera.backgroundColor = invertEffectIsOn ? Color.black : Color.white;
            playerSpriteRenderer.color = invertEffectIsOn ? Color.white : Color.black;
            gameScreen.SetColor(invertEffectIsOn);
            deathScreen.SetColor(invertEffectIsOn);
            resetGameButton.SetColor(invertEffectIsOn);
            uiGainedScoreColorInvert.SetColor(invertEffectIsOn);

            textFontAsset.material.SetColor("_UnderlayColor", invertEffectIsOn ? new Color(1f, 1f, 1f, 0.5f) : Color.black);
            textFontAsset.material.SetFloat("_UnderlayDilate", invertEffectIsOn ? -0.5f : 1f);

            hyperdriveSpeedlines.InvertSpeedlineColors(invertEffectIsOn);
            hyperdriveWallSection.SetColor(invertEffectIsOn ? Color.white : Color.black);

            MapController.Singleton.SetMapColor(invertEffectIsOn ? Color.white : Color.black);
            PlayerController.Singleton.Invert = invertEffectIsOn;
        }

        public void HyperdriveTravel()
        {
            cameraFollowAndTrack.enabled = false;

            // Reset the fuel:
            PlayerController.Singleton.AddFuel(float.MaxValue / 2f);
            PlayerController.Singleton.AddPlayerEffect(PlayerController.PlayerEffect.Hyperdrive, float.MaxValue);

            MapController.Singleton.OpenCloseMap(true);

            StartCoroutine("HyperdriveEffect");
        }

        private IEnumerator HyperdriveEffect()
        {
            float animationTime = hyperdriveAnimationTime;

            while (animationTime > 0f)
            {
                animationTime -= Time.deltaTime;

                playerTransform.right = Vector3.Lerp(playerTransform.right, Vector3.right, 1f - animationTime / hyperdriveAnimationTime);

                yield return new WaitForEndOfFrame();
            }

            playerAnimator.Play("Hyperdrive_Begin");

            yield return new WaitForSeconds(0.5f);

            // Give the player control over the hyperdrive travel, giving them the ability to collect score.
            PlayerController.Singleton.AddPlayerEffect(PlayerController.PlayerEffect.HyperdriveController, float.MaxValue);

            // Play the hyperdrive travel effects:
            hyperdriveSpeedlines.Play();
            hyperdriveWallSection.Play();

            float travelTimeLeft = hyperdriveTravelTime;
            float travelDistance = Random.Range(hyperdriveMinDistance, hyperdriveMaxDistance);

            while (travelTimeLeft > 0f)
            {
                travelTimeLeft -= Time.deltaTime;

                // Increase performance:
                float frameDistance = travelDistance * (Time.deltaTime / hyperdriveTravelTime);

                // Update stats:
                ScoreController.Singleton.AddDistanceTraveled(frameDistance);
                ScoreController.Singleton.AddStatRecordValue(ScoreController.StatRecordType.HyperdriveDistanceTraveled, frameDistance);

                // Modify the fuel bar to represent time left on hyperdrive travel:
                PlayerController.Singleton.UpdateFuelBar(travelTimeLeft / hyperdriveTravelTime);

                yield return new WaitForEndOfFrame();
            }

            // The hyperdrive travel is done, stop all effects:
            hyperdriveSpeedlines.Stop();
            hyperdriveWallSection.Stop();

            playerCollider.enabled = false;

            // Remove the hyperdrive travel controller effect:
            PlayerController.Singleton.RemovePlayerEffect(PlayerController.PlayerEffect.HyperdriveController);

            playerAnimator.SetTrigger("Exit Hyperdrive");

            // Rebuild the map:
            MapController.Singleton.RebuildMap(MapController.Singleton.PlayerSeeRadius * 3f, hyperdriveEntrySectionTemplate);

            // Cool off the engine, aka let the player drift into the map again.
            float coolOffWaitTime = hyperdriveWaitTime;

            while (coolOffWaitTime > 0f)
            {
                coolOffWaitTime -= Time.deltaTime;

                playerTransform.position = Vector3.Lerp(playerTransform.position, Vector3.zero, 1f - coolOffWaitTime / hyperdriveWaitTime);

                PlayerController.Singleton.UpdateFuelBar(1f - coolOffWaitTime / hyperdriveWaitTime);

                MapController.Singleton.MoveMap(new Vector3(15f, 0f) * Time.deltaTime);

                yield return new WaitForEndOfFrame();
            }

            // Actually pool the walls:
            hyperdriveWallSection.ResetSection();

            // Give back control of the player to the user:
            PlayerController.Singleton.RemovePlayerEffect(PlayerController.PlayerEffect.Hyperdrive);
            playerCollider.enabled       = true;
            cameraFollowAndTrack.enabled = true;
        }

    }

}
