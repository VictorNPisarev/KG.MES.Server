window.socketService = {
	socket: null,
	dotnetRef: null,

	connect: function (serverUrl) {
		console.log('');
		console.log('Socket connecting to:', serverUrl);
		console.log('');
		this.socket = io(serverUrl, {
			transports: ['websocket', 'polling']
		});

		this.socket.on('connect', () => {
			console.log('Socket connected');
		});

		this.socket.on('disconnect', () => {
			console.log('Socket disconnected');
		});
	},

	onMessage: function (dotnetRef) {
		this.dotnetRef = dotnetRef;
	},

	subscribe: function (channel, id) {
		if (this.socket) {
			var room = id ? channel + ':' + id : channel;
			console.log('Subscribing to:', room);
			this.socket.emit('subscribe:' + channel, id || '');

			this.socket.on(channel + ':updated', (data) => {
				console.log('Received ' + channel + ':updated:', data);
				if (this.dotnetRef) {
					this.dotnetRef.invokeMethodAsync('OnSocketMessage', channel, data);
				}
			});
		}
	},

	unsubscribe: function (channel) {
		if (this.socket) {
			this.socket.emit('unsubscribe:' + channel);
			this.socket.off(channel + ':updated');
		}
	},

	disconnect: function () {
		if (this.socket) {
			this.socket.disconnect();
			this.socket = null;
		}
	}
};