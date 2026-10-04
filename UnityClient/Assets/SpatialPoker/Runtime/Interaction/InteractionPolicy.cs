using UnityEngine;

namespace SpatialPoker.Interaction
{
    public enum RevealPolicy
    {
        Public,
        OwnerOnly,
        Never,
        ServerControlled
    }

    public enum AuthorityMode
    {
        ClientVisualOnly,
        ServerAuthoritative
    }

    public enum GrabMode
    {
        Disabled,
        Full,
        Cosmetic
    }

    [CreateAssetMenu(
        fileName = "InteractionPolicy",
        menuName = "Spatial Poker/Interaction Policy")]
    public sealed class InteractionPolicy : ScriptableObject
    {
        [SerializeField] private bool canHover = true;
        [SerializeField] private bool canTouch = true;
        [SerializeField] private GrabMode grabMode = GrabMode.Disabled;
        [SerializeField] private bool canDrag;
        [SerializeField] private bool canRotate;
        [SerializeField] private bool canFlip;
        [SerializeField] private bool ownerOnly;
        [SerializeField] private RevealPolicy revealPolicy = RevealPolicy.Never;
        [SerializeField] private AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative;
        [Min(0f)]
        [SerializeField] private float maxCosmeticGrabDistance = 0.025f;

        public bool CanHover => canHover;
        public bool CanTouch => canTouch;
        public GrabMode GrabMode => grabMode;
        public bool CanDrag => canDrag;
        public bool CanRotate => canRotate;
        public bool CanFlip => canFlip;
        public bool OwnerOnly => ownerOnly;
        public RevealPolicy RevealPolicy => revealPolicy;
        public AuthorityMode AuthorityMode => authorityMode;
        public float MaxCosmeticGrabDistance => maxCosmeticGrabDistance;
    }
}
