using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryCounter : BaseCounter {


    [SerializeField] private int teamId;


    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                // Only accepts Plates

                DeliveryManager.Instance.DeliverRecipe(plateKitchenObject, teamId, transform.position);

                KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            }
        }
    }

}