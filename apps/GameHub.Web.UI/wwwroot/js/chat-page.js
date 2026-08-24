window.chatChannelPage = {
    _messageObserver: null,
    _memberObserver: null,
    _messagesContainer: null,
    _messagesScrollHandler: null,
    _messagesScrollFrame: null,
    _wasNearMessagesBottom: null,

    init: function (dotnet, messagesContainer, messagesTopSentinel, membersContainer, membersBottomSentinel) {
        this.dispose();

        if (messagesContainer && messagesTopSentinel) {
            this._messagesContainer = messagesContainer;
            this._messageObserver = new IntersectionObserver(
                entries => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            dotnet.invokeMethodAsync('OnMessagesTopReached');
                        }
                    });
                },
                {
                    root: messagesContainer,
                    threshold: 0.1
                }
            );

            this._messageObserver.observe(messagesTopSentinel);

            this._messagesScrollHandler = () => {
                if (this._messagesScrollFrame !== null) return;

                this._messagesScrollFrame = requestAnimationFrame(() => {
                    this._messagesScrollFrame = null;
                    const isNearBottom = this.isNearBottom(messagesContainer);

                    if (isNearBottom === this._wasNearMessagesBottom) return;

                    this._wasNearMessagesBottom = isNearBottom;
                    dotnet.invokeMethodAsync('OnMessagesBottomStateChanged', isNearBottom)
                        .catch(() => { });
                });
            };

            messagesContainer.addEventListener('scroll', this._messagesScrollHandler, { passive: true });
            this._messagesScrollHandler();
        }

        if (membersContainer && membersBottomSentinel) {
            this._memberObserver = new IntersectionObserver(
                entries => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            dotnet.invokeMethodAsync('OnMembersBottomReached');
                        }
                    });
                },
                {
                    root: membersContainer,
                    threshold: 0.1
                }
            );

            this._memberObserver.observe(membersBottomSentinel);
        }
    },

    dispose: function () {
        if (this._messageObserver) {
            this._messageObserver.disconnect();
            this._messageObserver = null;
        }

        if (this._memberObserver) {
            this._memberObserver.disconnect();
            this._memberObserver = null;
        }

        if (this._messagesContainer && this._messagesScrollHandler) {
            this._messagesContainer.removeEventListener('scroll', this._messagesScrollHandler);
        }

        if (this._messagesScrollFrame !== null) {
            cancelAnimationFrame(this._messagesScrollFrame);
        }

        this._messagesContainer = null;
        this._messagesScrollHandler = null;
        this._messagesScrollFrame = null;
        this._wasNearMessagesBottom = null;
    },

    scrollToBottom: function (container, smooth) {
        if (!container) return;
        container.scrollTo({
            top: container.scrollHeight,
            behavior: smooth ? 'smooth' : 'auto'
        });
    },

    isNearBottom: function (container) {
        if (!container) return true;

        const threshold = 96;
        const remainingDistance = container.scrollHeight - container.scrollTop - container.clientHeight;
        return remainingDistance <= threshold;
    },

    getScrollHeight: function (container) {
        if (!container) return 0;
        return container.scrollHeight;
    },

    restoreScrollAfterPrepend: function (container, previousScrollHeight) {
        if (!container) return;

        const newScrollHeight = container.scrollHeight;
        const delta = newScrollHeight - previousScrollHeight;
        container.scrollTop = container.scrollTop + delta;
    }
};
