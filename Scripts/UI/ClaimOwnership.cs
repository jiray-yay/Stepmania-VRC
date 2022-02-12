
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Simple button that claims the ownership of an object, currently unused but useful for tests
    /// </summary>
    public class ClaimOwnership : UdonSharpBehaviour
    {
        public GameObject toClaim;

        void Interact()
        {
            Networking.SetOwner(Networking.LocalPlayer, toClaim);
        }
    }
}
