.HLL 'p5'

.sub load :load :anon
  get_class $P0, 'Ref'
  subclass $P1, $P0, 'P5Scalar'
.end

.namespace ['P5Scalar']
