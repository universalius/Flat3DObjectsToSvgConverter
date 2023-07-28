//import { SvgNest } from './svgnest';
const { SvgNest } = require('./svgnest');

module.exports = function (callback, svgString) {
    var svgNest = new SvgNest();
    var svg = svgNest.parsesvg(svgString);
    //SvgNest.start(progress, renderSvg);
    callback(/* error */ null, svg);
};