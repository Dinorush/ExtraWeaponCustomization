using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EWC.Utils.Structs
{
    public struct pReplicator<T> where T : Il2CppSystem.Object
    {
        private SNetStructs.pReplicator pRep;

        public bool IsValid => pRep.IsValid();

        public readonly ushort ReplicatorKey => pRep.keyPlusOne;

        public bool TryGet([MaybeNullWhen(false)] out T comp)
        {
            if (pRep.TryGetID(out var rep) && rep != null && rep.ReplicatorSupplier != null)
            {
                comp = rep.ReplicatorSupplier.TryCast<T>();
                return comp != null;
            }
            comp = null;
            return false;
        }

        public void Set(IReplicator? rep) => pRep.SetID(rep);
    }

    public struct pSentryGun
    {
        private pReplicator<SentryGunInstance> pRep;

        public bool TryGet([MaybeNullWhen(false)] out SentryGunInstance comp) => pRep.TryGet(out comp);

        public void Set(SentryGunInstance comp)
        {
            if (comp != null)
                pRep.Set(comp.Replicator);
            else
                pRep.Set(null);
        }
    }
}
