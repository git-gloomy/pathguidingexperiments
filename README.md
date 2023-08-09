Setup for the comparison of different path guiding optimizations. The implementation is based on [OpenPGL.NET](https://github.com/pgrit/OpenPGL.NET) and [SeeSharp](https://github.com/pgrit/SeeSharp) by [Pascal Grittmann](https://github.com/pgrit).

## Building

Precompiled binaries for openpgl and its dependencies can be downloaded by running the `make.ps1` (all platforms) or `make.sh` (Linux and OSX) scripts. If you want to supply your own binaries, check these scripts for details.

## References

Currently, this project focusses on several methods of optimizing the guiding probability. Current implementations are based on:
- Jiří Vorba, Johannes Hanika, Sebastian Herholz, Thomas Müller, Jaroslav Křivánek, and Alexander Keller. 2019. Path guiding in production. In ACM SIGGRAPH 2019 Courses (SIGGRAPH '19). Article 18, 1–77. [https://doi.org/10.1145/3305366.3328091]
- Pascal Grittmann, Ömercan Yazici, Iliyan Georgiev, and Philipp Slusallek. 2022. Efficiency-aware multiple importance sampling for bidirectional rendering algorithms. ACM Trans. Graph. 41, 4, Article 80 (July 2022), 12 pages. [https://doi.org/10.1145/3528223.3530126]
- Mateu Sbert, Vlastimil Havran and László Szirmay-Kalos. 2019. Optimal Deterministic Mixture Sampling. Eurographics 2019 - Short Papers. [https://diglib.eg.org/handle/10.2312/egs20191018]
