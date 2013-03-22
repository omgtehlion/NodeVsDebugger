module.exports = {
    onload: function () {
        var SET_BREAKPOINT_HERE = __filename + " loaded";
        console.log(SET_BREAKPOINT_HERE);
    },
    print: function (msg) {
        console.log(msg);
    }
};