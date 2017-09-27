    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
    {
        # System update will prevent updating modified date, modified by and no event firing
        item.SystemUpdate(false);
        updatedItemCount++;
    }


    class DisabledItemEventsScope : SPItemEventReceiver, IDisposable
    {
        bool oldValue;

        public DisabledItemEventsScope()
        {
            this.oldValue = base.EventFiringEnabled;
            base.EventFiringEnabled = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            base.EventFiringEnabled = oldValue;
        }

        #endregion
    }
