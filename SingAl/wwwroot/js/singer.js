var app = new Vue({
    el: '#app',
    data: {
        state: 'init',
        search: '',
        nickname: '',
        songResults: [],
        singerId: null
    },
    methods: {
        start: async function (event) {
            if (this.nickname.trim() != '') {
                var result = await postJson('/join', { nickname: this.nickname });
                if (result.ok) {
                    var data = await result.json();
                    this.singerId = data.singerId;
                    this.state = 'home';
                }
            }
        },

        doSearch: async function (val, oldVal) {
            if (typeof (val) == 'string' && val.trim().length > 0 && val != oldVal) {
                var result = await fetch('/search?' + new URLSearchParams({ query: val.trim() }))
                if (result.ok) {
                    this.songResults = await result.json();
                }
            }
        },

        enqueue: async function (song) {
            var result = await postJson('/addsong', { singerId: this.singerId, songId: song.id });
        },

        pause: async function () {
            var result = await postJson('/pause', { singerId: this.singerId });
        },

        play: async function () {
            var result = await postJson('/play', { singerId: this.singerId });
        },

        skip: async function () {
            var result = await postJson('/skip', { singerId: this.singerId });
        },

    },
    watch: {
        search: function (val, oldVal) {
            this.doSearch(val, oldVal);
        }
    }
});