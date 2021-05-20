var app = new Vue({
    el: '#app',
    data: {
        currentSong: null,
        currentSinger: null,
        nextSong: null,
        nextSinger: null,
        songLine1: null,
        songLine2: null,
        songLine1Index: 0,
        songLine2Index: 0,
        singerName: "",
        lyricsTimerInterval: null,
        signalRConnection: null,
        video: "/video?index=0",
        audio: "",
        currentSongLyrics: null,
        processedLyrics: null,
        lyricIndex: -1,
        showTitle: false,
        nextScreenLine: 0,
        state: 'stopped',
    },
    methods: {
        onSongAdded: function (singer, song) {
            if (!this.currentSong) {
                this.dequeueAndPlayNext();
            } else {
                var showNext = async () => {
                    var result = await fetch("/queue");
                    if (result.ok) {
                        var queue = await result.json();
                        if (queue && queue.length > 0) {
                            this.nextSong = queue[0].song;
                            this.nextSinger = queue[0].singer;
                        }
                    }
                }

                showNext();
            }
        },

        dequeueAndPlayNext: async function () {
            var result = await fetch("/dequeue");
            if (result.ok) {
                var queuedSong = await result.json();
                if (queuedSong.song) {
                    this.startSong(queuedSong.singer, queuedSong.song);
                }
                if (queuedSong.next && queuedSong.next.length > 0) {
                    this.nextSinger = queuedSong.next[0].singer;
                    this.nextSong = queuedSong.next[0].song;
                }
                else {
                    this.nextSinger = null;
                    this.nextSong = null;
                }
            }
        },

        startSong: async function (singer, song) {
            if (!song) return;
            this.currentSong = song;
            this.currentSinger = singer;
            const lyricsResult = await fetch("/lyrics?songId=" + song.id);
            if (lyricsResult.ok) {
                this.currentSongLyrics = await lyricsResult.json();
                this.processedLyrics = this.preProcessLyrics(this.currentSongLyrics);
                this.audio = "/songAudio?songId=" + song.id;
                //await this.$refs.audioPlayer.play();
                this.showTitle = true;
                this.titleStart = null;
                this.songLine1 = null;
                this.songLine2 = null;
                this.nextScreenLine = 0;
                this.lyricIndex = -1;
                this.state = 'playing';
            }
        },

        startSignalR: async function () {
            try {
                await this.signalRConnection.start();
                console.log("SignalR Connected.");
            } catch (err) {
                console.log(err);
                setTimeout(this.startSignalR, 5000);
            }
        },

        renderLyric: function (data) {
            return data.map((c) => ({ highlight: 0, text: c.text, time: c.time, duration: c.duration }));
        },

        updateHighlight: function (songLine, time) {
            var status = 0;
            for (var i = 0; i < songLine.length; i++) {
                const chunk = songLine[i];
                if (time > chunk.time + chunk.duration) {
                    chunk.highlight = "100%";
                    status = 2;
                }
                else if (time < chunk.time) {
                    chunk.highlight = "0%";
                    status = 0;
                }
                else{
                    const hl = Math.round(((time - chunk.time) / chunk.duration) * 100);
                    chunk.highlight = `${hl}%`
                    status = 1;
                }
            }
            return status;
        },

        moveToNextLyric: function () {
            if (this.lyricIndex + 1 >= this.processedLyrics.length) {
                return;
            }
            this.lyricIndex++;
            const rendered = this.renderLyric(this.processedLyrics[this.lyricIndex].data);
            if (this.nextScreenLine == 0) {
                this.songLine1 = rendered;
            }
            else {
                this.songLine2 = rendered;
            }
            this.nextScreenLine = this.nextScreenLine == 1 ? 0 : 1;
        },

        perTick: function () {
            const player = this.$refs.audioPlayer;
            if (this.state == 'playing' && this.processedLyrics.length > 0 && player.readyState == 4 && !player.paused && !player.ended) {
                const time = this.$refs.audioPlayer.currentTime;

                const timeOfNextLine = this.lyricIndex + 1 >= this.processedLyrics.length ? 10 : this.processedLyrics[this.lyricIndex + 1].time;

                // check if title needs hiding
                if (this.lyricIndex < 0 && time > max(timeOfNextLine - 8, 8)) {
                    this.showTitle = false;
                }

                // show first lines after title
                if (!this.songLine1 && time > timeOfNextLine - 5) {
                    this.moveToNextLyric();
                    if (this.processedLyrics.length > 1) {
                        this.moveToNextLyric();
                    }
                }

                var s1Status = 0;
                var s2Status = 0;

                if (this.songLine1) {
                    s1Status = this.updateHighlight(this.songLine1, time);
                }
                if (this.songLine2) {
                    s2Status = this.updateHighlight(this.songLine2, time);
                }

                if ((s1Status == 2 || s2Status == 2) && timeOfNextLine - time < 5) {
                    this.moveToNextLyric();
                }
            }
            else {
                if (player.ended) {
                    this.currentSong = null;
                    this.dequeueAndPlayNext();
                }
            }
        },

        displayLine: function (line, idx, dbgTime, dbgText) {
            if (idx == 0) this.songLine1 = line;
            else if (idx == 1) this.songLine2 = line;
            console.log(`${dbgTime}: ${dbgText}`);
        },

        clearLines: function () {
            this.songLine1 = ''; this.songLine2 = '';
        },

        // this is where the magic happens
        preProcessLyrics: function (lyrics) {
            var result = new Array();

            var currentLineTime = 0;
            var lineData = new Array();
            var debugText = '';

            for (var i = 0; i < lyrics.length; i++) {
                const currentLyric = lyrics[i];
                const nextLyric = i < lyrics.length - 1 ? lyrics[i + 1] : null;
                var text = currentLyric.text;

                const duration = nextLyric ? nextLyric.seconds - currentLyric.seconds : 5;

                if (text) {
                    
                    if (text.charAt(0) == '\\') {

                        if (lineData.length > 0) {
                            result.push({cmd: 'line', time: currentLineTime, data: lineData, debugText });
                        }

                        // result.push({ cmd: 'clear', time: currentLyric.seconds - clearTimeDelta });    

                        currentLineTime = currentLyric.seconds;
                        lineData = new Array();
                        debugText = '';
                        text = text.substr(1);
                    }
                    else if (text.charAt(0) == '/') {
                        if (lineData.length > 0) {
                            result.push({ cmd: 'line', time: currentLineTime, data: lineData, debugText });
                        }
                        currentLineTime = currentLyric.seconds;
                        text = text.substr(1);
                        lineData = new Array();
                        debugText = '';
                        
                    }

                    const duration = (i > lyrics.length - 2 ? 10 : lyrics[i + 1].seconds) - currentLyric.seconds;
                    
                    // create text
                    lineData.push({ time: currentLyric.seconds, duration, text });
                    debugText += text;
                }
            }

            if (lineData.length > 0) {
                result.push({ cmd: 'line', time: currentLineTime, data: lineData, debugText });
            }

            return result;
        }

    },
    watch: {
        search: function (val, oldVal) {
            this.doSearch(val, oldVal);
        }
    },
    mounted: function () {
        const _this = this;
        this.signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl("/webplayerhub")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.signalRConnection.on("SongAdded", _this.onSongAdded );
        this.startSignalR();
        lyricsTimerInterval = setInterval(_this.perTick, 10);
    }
});