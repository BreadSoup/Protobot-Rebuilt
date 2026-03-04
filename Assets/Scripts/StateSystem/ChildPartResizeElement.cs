using UnityEngine;

namespace Protobot.StateSystems {
    /// <summary>
    /// Undo / redo element that restores a child-part (e.g. screw) to a previously
    /// recorded variant by destroying the current scene object and regenerating it.
    ///
    /// Unlike resize elements for aluminum or plates, this element cannot modify the
    /// part in-place because ChildPartGenerator builds full prefab instances that may
    /// differ structurally between variants. Instead it destroys the active object
    /// and recreates it from the stored generator + param value.
    ///
    /// Usage:
    /// <code>
    ///   // 1. Build both sides of the undo/redo pair BEFORE generating.
    ///   var pre  = new ChildPartResizeElement(gen, oldParam1, pos, rot);
    ///   var post = new ChildPartResizeElement(gen, newParam1, pos, rot);
    ///   pre.pair  = post;   // each element updates the other's objectToReplace
    ///   post.pair = pre;
    ///   StateSystem.AddElement(pre);
    ///   // 2. Generate new object and record it for undo destruction.
    ///   gen.param1.value = newParam1;
    ///   var newObj = gen.Generate(pos, rot);
    ///   pre.objectToReplace = newObj;   // undo destroys this
    ///   // 3. Destroy old object and push post state.
    ///   Object.Destroy(oldObj);
    ///   StateSystem.AddState(post);
    /// </code>
    /// </summary>
    public class ChildPartResizeElement : IElement {

        private readonly ChildPartGenerator generator;
        private readonly string             param1Value;
        private readonly string             param2Value; // null for single-param generators
        private readonly Vector3            position;
        private readonly Quaternion         rotation;

        /// <summary>
        /// The live scene GameObject to destroy when this element's Load() fires.
        /// Set by the caller after generation, and updated by Load() after each
        /// undo/redo so the chain stays correct for repeated undo/redo cycles.
        /// </summary>
        public GameObject objectToReplace;

        /// <summary>
        /// The paired element on the other side of the undo/redo pair.
        /// After Load() creates a new object it assigns that object to
        /// <c>pair.objectToReplace</c> so the partner always destroys the right thing.
        /// </summary>
        public ChildPartResizeElement pair;

        /// <param name="gen">Source generator (a persistent scene object).</param>
        /// <param name="p1">The param1 value this element restores (e.g. "2.25in").</param>
        /// <param name="p2">The param2 value this element restores, or null for single-param
        ///     generators (e.g. spacer size "1/4in" when param1 holds the material type).</param>
        /// <param name="pos">World position at which to recreate the part.</param>
        /// <param name="rot">World rotation at which to recreate the part.</param>
        public ChildPartResizeElement(ChildPartGenerator gen,
                                      string p1, string p2, Vector3 pos, Quaternion rot) {
            generator   = gen;
            param1Value = p1;
            param2Value = p2;
            position    = pos;
            rotation    = rot;
        }

        public void Load() {
            if (generator == null) return;

            // Destroy the object that this state supersedes.
            if (objectToReplace != null)
                Object.Destroy(objectToReplace);

            // Restore the stored param values and regenerate the part.
            generator.param1.value = param1Value;
            if (generator.UsesTwoParams && !string.IsNullOrEmpty(param2Value))
                generator.param2.value = param2Value;
            var newObj = generator.Generate(position, rotation);

            // Update the paired element so it destroys the newly created object
            // if it fires next (redo after undo, or undo after redo).
            if (pair != null)
                pair.objectToReplace = newObj;
        }
    }
}
