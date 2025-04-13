using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FacialController {
    public class HelperFuncs
    {
        public static string BSPrefix(string[] strs)
        {
            string prefix = "";
            if (strs.Length == 0)
            {
                return "";
            }
            foreach (string name in strs)
            {
                var parts = name.Split('.');
                prefix = parts[0] + ".";
                return prefix;
            }
            return prefix;
        }
    }

    public static class FacialControllerHelper {
        static public float WideLimit(this FacialControllerHandle1D handler) {
            foreach (FacialControllerCustomValue value in handler.m_customValue) {
                if (value.name == "WideLimit")
                    return value.value;
            }
            throw new KeyNotFoundException();
        }

        static public float BlinkLimit(this FacialControllerHandle1D handler)
        {
            foreach (FacialControllerCustomValue value in handler.m_customValue)
            {
                if (value.name == "BlinkLimit")
                    return value.value;
            }
            throw new KeyNotFoundException();
        }
    }
}
