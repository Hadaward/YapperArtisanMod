using UnityEngine;
using System;
using TMPro;

namespace Artisan.Features.Monsters
{
    public static class ArtisanDamageTextFactory
    {
        public static void Create(Vector3 position, int damage)
        {
            GameObject damageIndicator = new GameObject(
                "DamageText",
                new Type[2]
                {
                    typeof(TextMeshPro),
                    typeof(ArtisanDamageText3D)
                }
            );

            TextMeshPro damageText = damageIndicator.GetComponent<TextMeshPro>();
            ArtisanDamageText3D damageText3D = damageIndicator.GetComponent<ArtisanDamageText3D>();

            damageText.alignment = (TextAlignmentOptions)514;
            damageText.text = $"-{damage}";

            Color color = default(Color);
            float fontSize;
            float time;
            if (damage >= 50)
            {
                color = Color.red;
                fontSize = 6f;
                time = 3f;
            }
            else if (damage >= 25)
            {
                color = Color.yellow;
                fontSize = 5f;
                time = 2f;
            }
            else
            {
                color = new Color(1f, 0.5f, 0f);
                fontSize = 4f;
                time = 1.5f;
            }

            damageText.color = color;
            damageText.fontSize = fontSize;

            damageIndicator.transform.position = position;
            damageText3D.Setup(time);
        }
    }
}
