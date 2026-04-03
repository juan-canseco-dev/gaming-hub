window.chatChannelPage = {
    _messageObserver: null,
    _memberObserver: null,

    init: function (dotnet, messagesContainer, messagesTopSentinel, membersContainer, membersBottomSentinel) {
        this.dispose();

        if (messagesContainer && messagesTopSentinel) {
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
    },

    scrollToBottom: function (container) {
        if (!container) return;
        container.scrollTop = container.scrollHeight;
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