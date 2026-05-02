using UnityEngine;
using TMPEffects.Components;
using TMPEffects.CharacterData;

namespace LGSToolbox
{
    [RequireComponent(typeof(TMPWriter))]
    public class TypewriterSound : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private string voiceKey = "None";
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] private float volume = 1f;

        [Header("Filters")]
        [SerializeField] private bool ignoreSpaces = true;
        [SerializeField] private bool ignorePunctuation = true;

        private TMPWriter writer;

        private void Awake()
        {
            writer = GetComponent<TMPWriter>();
        }

        private void OnEnable()
        {
            if (writer != null)
                writer.OnCharacterShown.AddListener(OnCharacterShown);
        }

        private void OnDisable()
        {
            if (writer != null)
                writer.OnCharacterShown.RemoveListener(OnCharacterShown);
        }

        private void OnCharacterShown(TMPWriter sender, CharData charData)
        {
            char character = charData.info.character;

            if (ignoreSpaces && char.IsWhiteSpace(character))
                return;

            if (ignorePunctuation && char.IsPunctuation(character))
                return;

            float pitch = Random.Range(pitchRange.x, pitchRange.y);

            SoundManager.Play(voiceKey, volume, pitch);
        }
    }
}