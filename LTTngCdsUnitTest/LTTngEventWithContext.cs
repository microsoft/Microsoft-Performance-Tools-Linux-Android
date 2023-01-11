// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LTTngCds.CookerData;

namespace LTTngCdsUnitTest
{
    public class LTTngEventWithContext
    {
        public LTTngEventWithContext(LTTngEvent lTTngEvent, LTTngContext lTTngContext)
        {
            LTTngEvent = lTTngEvent;
            LTTngContext = lTTngContext;
        }

        public LTTngEvent LTTngEvent { get; private set; }
        public LTTngContext LTTngContext { get; private set; }
    }
}
