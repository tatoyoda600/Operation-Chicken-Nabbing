using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TestMovement : MonoBehaviour
{
    public PlayerInput playerInput;

    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;
    public Tilemap interactionTilemap;

    Vector2 movementDirection = Vector2.zero;
    Vector2 destination = Vector2.zero;

    private void Awake()
    {
        playerInput = new PlayerInput();
        transform.position = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(transform.position));
        destination = transform.position;

        playerInput.Interaction.Click.performed += (_) =>
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(playerInput.Interaction.MousePosition.ReadValue<Vector2>());
            Vector3Int gridPos = interactionTilemap.WorldToCell(pos);
            if (interactionTilemap.HasTile(gridPos))
            {
                destination = interactionTilemap.GetCellCenterWorld(gridPos);
            }
        };
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    private void Update()
    {
        Vector2 movement = (destination - (Vector2)transform.position);
        if (movement.magnitude < 0.5f * groundTilemap.cellSize.x)
        {
            movement = Vector2.zero;
            movementDirection = Vector2.zero;
            destination = transform.position;
        }
        movementDirection += movement.normalized * Time.deltaTime;
        OnMove(movementDirection);
    }

    private void OnMove(Vector2 direction)
    {
        Vector2Int intDirection = Vector2Int.RoundToInt(direction);
        if (intDirection != Vector2Int.zero)
        {
            movementDirection -= (Vector2)intDirection;
            Vector3Int gridPos = groundTilemap.WorldToCell(transform.position) + (Vector3Int)intDirection;
            if (!HasCollision(gridPos))
            {
                transform.position = groundTilemap.GetCellCenterWorld(gridPos);
            }
            else
            {
                ZoneDictionary.Zone? interactionZone = interactionTilemap.GetComponent<ZoneDictionary>().GetZone(gridPos);

                if (interactionZone?.zoneName.Equals("1") == true)
                {
                    int curValue = SiblingRuleTile.GetState(collisionTilemap, gridPos);
                    int newValue = curValue <= 0 ? 1 : 0;
                    foreach (Vector3Int zonePos in interactionZone?.gridPositions)
                    {
                        SiblingRuleTile.SetState(collisionTilemap, zonePos, newValue);
                    }
                }

                movementDirection = Vector2.zero;
                destination = transform.position;
            }
        }
    }

    private bool HasCollision(Vector3Int gridPos)
    {
        return !groundTilemap.HasTile(gridPos) || (collisionTilemap.HasTile(gridPos) && collisionTilemap.GetSprite(gridPos) != null);
    }
}
