using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Artisan.Features.Monsters
{
    public class ArtisanDamageText3D : MonoBehaviour
    {
        private TextMeshPro textComponent;

        private float timer = 1.5f;

        private Vector3 baseWorldPosition;

        private float floatHeight;

        private void Awake()
        {
            textComponent = ((Component)this).GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            baseWorldPosition = this.transform.position;

            if (Camera.main == null)
                return;

            this.transform.rotation = Quaternion.LookRotation(this.transform.position - Camera.main.transform.position);
        }

        private void Update()
        {
            floatHeight += Time.deltaTime * 0.8f;

            this.transform.position = baseWorldPosition + Vector3.up * floatHeight;

            if (Camera.main != null)
            {
                this.transform.rotation = Quaternion.LookRotation(this.transform.position - Camera.main.transform.position);
            }
            
            timer -= Time.deltaTime;

            if (timer > 0f && timer < 0.75f)
            {
                float scale = Mathf.Lerp(1f, 0.5f, (0.75f - timer) / 0.75f);
                this.transform.localScale = Vector3.one * scale;
            }

            if (textComponent != null)
            {
                textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, Mathf.Clamp01(timer * 2f));
            }

            if (timer <= 0f)
            {
                UnityEngine.Object.Destroy(this.gameObject);
            }
        }

        public void Setup(float time)
        {
            timer = time;
        }
    }
}
