using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCowTarget : MonoBehaviour
{
	[SerializeField] private bool autoCreateCollider = true;

	private void Awake()
	{
		EnsureColliderExists();
	}

	private void OnValidate()
	{
		EnsureColliderExists();
	}

	private void EnsureColliderExists()
	{
		if (!autoCreateCollider)
			return;

		if (GetComponent<Collider2D>() != null || GetComponent<Collider>() != null)
			return;

		SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		if (spriteRenderer != null && spriteRenderer.sprite != null)
		{
			BoxCollider2D boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
			Vector2 size = spriteRenderer.sprite.bounds.size;
			boxCollider2D.size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
			boxCollider2D.offset = spriteRenderer.sprite.bounds.center;
			return;
		}

		gameObject.AddComponent<BoxCollider2D>();
	}
}