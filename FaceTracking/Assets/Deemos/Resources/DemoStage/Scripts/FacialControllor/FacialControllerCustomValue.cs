using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FacialController {
    [Serializable]
    public class FacialControllerCustomValue {
        public string name;
        public float value;
        public FacialControllerCustomValue(string name, float value) {
            this.name = name;
            this.value = value;
        }
    }
}
